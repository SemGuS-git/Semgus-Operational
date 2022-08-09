using Semgus.MiniParser;
using Semgus.OrderSynthesis.IntervalSemantics;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;
    internal record MonoQueryBundle(FunctionDefinition output_transformer, IReadOnlyList<FunctionDefinition> preconditions, IReadOnlyList<int> relevant_block_ids);

    internal static class Util {
        public static Identifier MapLabelToTypeIdentifier(TypeLabel tl) => tl switch {
            TypeLabel.Int => IntType.Id,
            TypeLabel.Bool => BitType.Id,
            _ => throw new NotSupportedException(),
        };
    }

    [XmlRoot("hole_values")]
    public class HolesXmlDoc {
        public class Item {
            [XmlAttribute(AttributeName = "line")] public int Line { get; set; }
            [XmlAttribute(AttributeName = "value")] public int Value { get; set; }
        }

        [XmlElement("hole_value")] public List<Item> Items { get; set; }
    }

    internal class MonotonicityStep {
        IReadOnlyList<StructType> StructDefs { get; }
        IReadOnlyList<StructType> StructDefsToOrder { get; } // this is a proper subset of StructDefs
        IReadOnlyList<MonoQueryBundle> QueryFunctions { get; } // at most one per sem

        IReadOnlyDictionary<Identifier, (int prod_idx, int sem_idx)> QueryIdMap { get; }

        public MonotonicityStep(TupleLibrary stuff) {

            var struct_defs = stuff.TypeHelper.BlockTypes.Select(bt =>
                new StructType(
                    new($"bt_{bt.Id}"),
                    bt.Members.Select((label, i) =>
                        new Variable($"v{i}", Util.MapLabelToTypeIdentifier(label))
                    ).ToList()
                )
            ).ToList();

            Dictionary<Identifier, (int prod_idx, int sem_idx)> query_fn_id_map = new();

            List<MonoQueryBundle> query_fns = new();

            bool[] bt_occurs_as_output = new bool[struct_defs.Count];

            {
                List<(MonoQueryBundle bundle, HashSet<int> input_block_types, int i, int j)> wip = new();

                for (int i = 0; i < stuff.Productions.Count; i++) {
                    var prod = stuff.Productions[i];
                    for (int j = 0; j < prod.Semantics.Count; j++) {
                        var sem = prod.Semantics[j];
                        var fn_id = new Identifier($"prod_{i}_sem_{j}");
                        var bundle = GetMonoSubjectFunction(struct_defs, fn_id, sem);

                        // omit sems with only trivial transformers.
                        // - If directly passing output from a child term, no transformer step is required.
                        // - If assigning from variable-free formulas, e.g. in literal terminals, we implicitly construct a singular interval.

                        if (bundle.relevant_block_ids.Count > 0) {
                            wip.Add((bundle, bundle.relevant_block_ids.Select(k => sem.BlockTypes[k]).ToHashSet(), i, j));
                            bt_occurs_as_output[sem.BlockTypes[1]] = true;
                        }
                    }
                }

                // Some block types might never be produced as outputs; i.e., we will never construct nonsingular intervals in them.
                // Identify these block types, and ignore all query functions with strictly non-interval inputs.

                foreach (var (bundle, _, i, j) in wip.Where(tu => tu.input_block_types.Any(bt_id => bt_occurs_as_output[bt_id]))) {
                    query_fns.Add(bundle);
                    query_fn_id_map.Add(bundle.output_transformer.Id, (i, j));
                }
            }

            var structs_to_ignore = new HashSet<Identifier>();
            for (int i = 0; i < bt_occurs_as_output.Length; i++) if (!bt_occurs_as_output[i]) structs_to_ignore.Add(struct_defs[i].Id);

            Debug.Assert(!structs_to_ignore.Overlaps(query_fns.Select(qf => qf.output_transformer.Signature.ReturnTypeId)));

            StructDefs = struct_defs;
            StructDefsToOrder = struct_defs.Where(sd => !structs_to_ignore.Contains(sd.Id)).ToList();
            QueryFunctions = query_fns;
            QueryIdMap = query_fn_id_map;
        }


        public IEnumerable<IStatement> GetFile() {
            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            foreach (var st in StructDefs) {
                yield return st.GetStructDef();
            }
            foreach (var st in StructDefsToOrder) {
                yield return st.GetEqualityFunction();
                yield return st.GetCompareGenerator();
                yield return st.GetDisjunctGenerator();
                yield return st.GetNonEqualityHarness();
            }

            foreach (var q in QueryFunctions) {
                foreach (var fdef in q.preconditions.Append(q.output_transformer)) yield return fdef;
            }

            yield return GetMain(StructDefs, StructDefsToOrder, QueryFunctions);
        }

        static IReadOnlyDictionary<int, (Identifier fn_id, int arg_id)> ScanHoleLines(StreamReader reader) {
            Regex a = new(@"/\*#MONO (.+)\.([0-9]+)\*/", RegexOptions.Compiled);
            Dictionary<int, (Identifier fn_id, int arg_id)> line_map = new();
            int i = 1;

            while (reader.ReadLine() is string line) {
                var match = a.Match(line);
                if (match.Success) {
                    var fn_id = new Identifier(match.Groups[1].Value);
                    var arg_id = int.Parse(match.Groups[2].Value);

                    line_map.Add(i, (fn_id, arg_id));
                }
                i++;
            }
            return line_map;
        }

        static IReadOnlyDictionary<Identifier, Monotonicity[]> ExtractMonotonicities(
            HolesXmlDoc doc,
            IReadOnlyDictionary<Identifier, FunctionDefinition> query_functions,
            IReadOnlyDictionary<int, (Identifier fn_id, int arg_id)> line_map
        ) {
            HashSet<int> consumed_lines = new();

            var d_mono = new Dictionary<Identifier, Monotonicity[]>();

            foreach (var line in doc.Items) {
                if (line_map.TryGetValue(line.Line, out var target)) {
                    Debug.Assert(consumed_lines.Add(line.Line)); // Each relevant line should only have one hole

                    var fdef = query_functions[target.fn_id];

                    Monotonicity[] mono;
                    if (!d_mono.TryGetValue(target.fn_id, out mono)) {
                        mono = new Monotonicity[fdef.Signature.Args.Count + 1]; // include the output slot as having constant monotonicity

                        // Initialize values to constant.
                        // So, if a particular arg is not tested, we assume it is constant wrt its semantics.
                        Array.Fill(mono, Monotonicity.Constant);
                        d_mono.Add(target.fn_id, mono);
                    }

                    mono[target.arg_id] = line.Value switch {
                        0 => Monotonicity.Increasing,
                        1 => Monotonicity.Decreasing,
                        _ => Monotonicity.None
                    };
                }
            }

            return d_mono;
        }
        static FunctionDefinition GetMain(
            IReadOnlyList<StructType> tuple_defs,
            IReadOnlyList<StructType> tuple_defs_to_order,
            IReadOnlyList<MonoQueryBundle> query_subjects
        ) {
            var instances_needed_per_type = new Dictionary<Identifier, int>();

            static int map_block_id(int i) => i > 1 ? i - 1 : i;

            // at least 3 instances are needed to check transitivity
            foreach (var t_t in tuple_defs_to_order) {
                instances_needed_per_type.Add(t_t.Id, 3);
            }

            foreach(var t_t in tuple_defs) {
                instances_needed_per_type.TryAdd(t_t.Id, 0);
            }

            foreach (var subject in query_subjects) {
                var counter = new Counter<Identifier>();
                var tf_args = subject.output_transformer.Signature.Args;
                for (int i = 0; i < tf_args.Count; i++) {
                    counter.Increment(tf_args[i].TypeId);
                }
                foreach (var id in subject.relevant_block_ids.Select(map_block_id).Select(j => tf_args[j].TypeId).Distinct()) {
                    counter.Increment(id);
                }
                foreach (var kvp in counter) {
                    if (instances_needed_per_type[kvp.Key] < kvp.Value) {
                        instances_needed_per_type[kvp.Key] = kvp.Value;
                    }
                }
            }

            var instances = new Dictionary<Identifier, IReadOnlyList<Variable>>();

            foreach (var kvp in instances_needed_per_type) {
                instances.Add(kvp.Key, Enumerable.Range(0, kvp.Value).Select(i => new Variable($"{kvp.Key}_{i}", kvp.Key)).ToList());
            }

            var raw_outer_args = new List<FunctionArg>();
            var body = new List<IStatement>();

            foreach (var st in tuple_defs) {
                var l = instances[st.Id];
                foreach (var v in l) {
                    st.PutConstructionForInputBlock(raw_outer_args, body, v);
                }
            }
            foreach(var st in tuple_defs_to_order) {
                var l = instances[st.Id];
                body.AddRange(st.GetPartialOrderAssertions(l[0], l[1], l[2]));
            }

            var cost = new Variable("cost", IntType.Id);
            body.Add(cost.Declare(Lit0));

            var sd_dict = tuple_defs.ToDictionary(a => a.Id);

            foreach (var a in query_subjects) {
                body.AddRange(GetMonoAssertions(cost, sd_dict, instances, a));
            }

            body.Add(new MinimizeStatement(cost.Ref()));

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("main"), raw_outer_args), body);
        }


        static MonoQueryBundle GetMonoSubjectFunction(IReadOnlyList<StructType> struct_defs, Identifier fn_id, BlockSemantics sem) {
            List<(FunctionArg, StructType)> pre_list = sem.BlockTypes.Select((bt, i) =>
                 (new FunctionArg(new($"b{i}"), struct_defs[bt].Id), struct_defs[bt])
            ).ToList();

            List<FunctionDefinition> preconditions = new();

            var ns = new FunctionNamespace(pre_list);

            var args = pre_list.Select(a => a.Item1).ToList();

            List<IStatement> steps = new();

            bool did_set_output = false;

            List<int> transformer_req_block_ids = null;

            StructType get_struct_def(int block_arg_idx) {
                return struct_defs[sem.BlockTypes[block_arg_idx]];
            }

            void assert_predicate(BlockAssert assert) {
                preconditions.Add(new(new(FunctionModifier.None, BitType.Id, new($"{fn_id}_pred_{preconditions.Count}"), args.SkipAt(1).ToList()),
                    new ReturnStatement(ns.Convert(assert.Expression))
                ));
            }

            void assign_output_from_formulas(BlockAssign assign) {
                if (assign.TargetBlockId != 1) throw new NotSupportedException();
                if (transformer_req_block_ids is not null) throw new InvalidOperationException();
                transformer_req_block_ids = assign.RequiredBlocks.ToList();


                var output_block_type = get_struct_def(assign.TargetBlockId);

                var ret = output_block_type.New(assign.Exprs.Select(
                        (v, i) => output_block_type.Elements[i].Assign(ns.Convert(v))
                    ).ToList()
                );

                steps.Add(new ReturnStatement(ret));
            }
            void assign_output_from_block(int src_id) {
                if (transformer_req_block_ids is not null) throw new InvalidOperationException();
                transformer_req_block_ids = new();

                steps.Add(new ReturnStatement(args[src_id].Ref()));
            }

            foreach (var step in sem.Steps) {
                switch (step) {
                    case BlockEval eval:
                        switch (eval.OutputBlockId) {
                            case 0:
                                throw new InvalidDataException();
                            case 1:
                                assign_output_from_block(eval.InputBlockId);
                                break;
                            default:
                                // Ignore evaluations that assign blocks other than the sem output
                                break;
                        }
                        break;
                    case BlockAssert assert:
                        assert_predicate(assert);
                        break;
                    case BlockAssign assign:
                        assign_output_from_formulas(assign);
                        break;
                }
            }

            Debug.Assert(transformer_req_block_ids is not null);
            var tf = new FunctionDefinition(new(FunctionModifier.None, /*BitType.Id*/ args[1].TypeId, fn_id, args.SkipAt(1).ToList()), steps);
            return new(tf, preconditions, transformer_req_block_ids);
        }

        private static IEnumerable<IStatement> GetMonoAssertions(
            Variable cost,
            IReadOnlyDictionary<Identifier, StructType> types,
            IReadOnlyDictionary<Identifier, IReadOnlyList<Variable>> instances,
            MonoQueryBundle query
        ) {
            var sig = query.output_transformer.Signature;

            static int map_block_id(int i) => i > 1 ? i - 1 : i;

            foreach (var target_idx_raw in query.relevant_block_ids) {
                var mono_flag = new Variable($"{query.output_transformer.Id}_mono_{target_idx_raw}", IntType.Id);
                yield return mono_flag.Declare(new Hole(2, $"#MONO {query.output_transformer.Id}.{target_idx_raw}"));

                var target_idx = map_block_id(target_idx_raw);

                var args = sig.Args;
                var n = args.Count;

                var target_st = types[args[target_idx].TypeId];
                var output_st = types[sig.ReturnTypeId];


                var arg_list_a = new IExpression[n];
                var arg_list_b = new IExpression[n];

                var ctr = new Counter<Identifier>();

                IExpression a_in = null, b_in = null;

                for (int i = 0; i < n; i++) {
                    var u = args[i].TypeId;

                    arg_list_a[i] = instances[u][ctr.Increment(u) - 1].Ref();
                    if (i == target_idx) {
                        arg_list_b[i] = instances[u][ctr.Increment(u) - 1].Ref();
                        a_in = arg_list_a[i];
                        b_in = arg_list_b[i];
                    } else {
                        arg_list_b[i] = arg_list_a[i];
                    }
                }

                Debug.Assert(a_in is not null && b_in is not null);

                IExpression inputs_ord = target_st.CompareId.Call(a_in, b_in);
                var guard = query.preconditions.Count == 0 ? inputs_ord :
                    Op.And.Of(query.preconditions.SelectMany(p => new IExpression[] { p.Call(arg_list_a), p.Call(arg_list_b) }).Append(inputs_ord).ToList());

                var a_out = query.output_transformer.Call(arg_list_a);
                var b_out = query.output_transformer.Call(arg_list_b);

                yield return mono_flag.IfEq(Lit0,
                    Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(a_out, b_out)).Assert()
                ).ElseIf(Op.Eq.Of(mono_flag.Ref(), Lit1),
                    Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(b_out, a_out)).Assert()
                ).Else(
                    cost.Assign(Op.Plus.Of(cost.Ref(), Lit1))
                );

            }
        }
        public record Output(IReadOnlyList<StructType> StructDefs, IReadOnlyList<StructType> StructDefsToOrder, IReadOnlyList<FunctionDefinition> Comparisons, IReadOnlyList<AnnotatedQueryFunction> QueryFunctions) {
            public IEnumerable<(StructType st, FunctionDefinition? cmp)> ZipComparisonsToTypes() {

                var a = Comparisons.ToDictionary(c => c.Id);

                return StructDefs.Select(st => (st, a.TryGetValue(st.CompareId, out var cmp) ? cmp : null));
            }
        }

        public async Task<Output> Execute(FlexPath dir, bool reuse_previous = false) {
            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            var file_mono = dir.Append("result.mono.json");
            var file_cmp = dir.Append("result.comparisons.sk");

            if (reuse_previous) {
                System.Console.WriteLine($"--- [Initial] Reusing prior Sketch output from {file_out} ---");
            } else {
                Directory.CreateDirectory(dir.PathWin);

                System.Console.WriteLine($"--- [Initial] Writing input file at {file_in} ---");
                WriteSketchInputFile(file_in);

                System.Console.WriteLine($"--- [Initial] Invoking Sketch on {file_in} ---");

                var sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

                if (sketch_result) {
                    Console.WriteLine($"--- [Initial] Sketch succeeded ---");
                } else {
                    Console.WriteLine($"--- [Initial] Sketch rejected; halting ---");
                    throw new Exception("Sketch rejected");
                }
            }

            Console.WriteLine($"--- [Initial] Extracting monotonicities ---");

            var mono = InspectMonotonicities(file_in, file_holes);
            Debug.Assert(mono.Count == QueryFunctions.Count, "Missing monotonicity labels for some semantics; halting");

            var annotated = QueryFunctions.Select(qf => {
                var id = qf.output_transformer.Id;
                return new AnnotatedQueryFunction(qf, QueryIdMap[id], mono[id]);
            }).ToList();

            Console.WriteLine($"--- [Initial] Reading compare functions ---");

            var compare_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), this.StructDefsToOrder.Select(s => s.CompareId));

            Debug.Assert(compare_functions.Count == this.StructDefsToOrder.Count, "Failed to extract all comparison functions; halting");

            Console.WriteLine($"--- [Initial] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted = PipelineUtil.ReduceEachToSingleExpression(compare_functions); // May throw

            return new(StructDefs, StructDefsToOrder, compacted, annotated);
        }

        private IReadOnlyDictionary<Identifier, Monotonicity[]> InspectMonotonicities(FlexPath file_in, FlexPath file_holes) {
            var ser = new XmlSerializer(typeof(HolesXmlDoc));

            IReadOnlyDictionary<int, (Identifier fn_id, int arg_id)> line_map;

            using (var sr = new StreamReader(file_in.PathWin)) {
                line_map = ScanHoleLines(sr);
            }

            HolesXmlDoc doc;

            using (var fs = new FileStream(file_holes.PathWin, FileMode.Open)) {
                doc = (HolesXmlDoc)ser.Deserialize(fs)!;
            }

            return ExtractMonotonicities(doc, this.QueryFunctions.ToDictionary(a => a.output_transformer.Id, a => a.output_transformer), line_map);
        }

        private void WriteSketchInputFile(FlexPath file_in) {
            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }
        }
    }
    internal record AnnotatedQueryFunction(MonoQueryBundle query, (int prod_idx, int sem_idx) sem_addr, IReadOnlyList<Monotonicity> mono);

}
