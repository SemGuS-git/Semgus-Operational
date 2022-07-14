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

    class FunctionNamespace2 {
        public Dictionary<BlockItemRef, ISettable> VarMap { get; }

        public FunctionNamespace2(IEnumerable<(FunctionArg, StructType)> args) {
            var var_map = new Dictionary<BlockItemRef, ISettable>();

            int i = 0;
            foreach (var arg in args) {
                for (int j = 0; j < arg.Item2.Elements.Count; j++) {
                    var item = arg.Item2.Elements[j];
                    var_map.Add(new(i, j), arg.Item1.Variable.Get(item.Id));
                }
                i++;
            }

            VarMap = var_map;
        }

        public IExpression Convert(IBlockExpression expression) => expression switch {
            BlockExprRead read => VarMap[read.Address],
            BlockExprLiteral lit => new Literal(lit.Value),
            BlockExprCall call => ConvertCall(call),
            _ => throw new NotSupportedException()
        };

        private IExpression ConvertCall(BlockExprCall call) {
            // Special cases
            switch (call.FunctionName) {
                case "ite":
                    if (call.Args.Count != 3) throw new InvalidDataException();
                    return new Ternary(Convert(call.Args[0]), Convert(call.Args[1]), Convert(call.Args[2]));
                case "true":
                    if (call.Args.Count != 0) throw new InvalidDataException();
                    return new Literal(1);
                case "false":
                    if (call.Args.Count != 0) throw new InvalidDataException();
                    return new Literal(0);
            }

            if (call.Args.Count == 1 && GetUnaryOpOrNull(call.FunctionName) is UnaryOp un_op) {
                return new UnaryOperation(un_op, Convert(call.Args[0]));
            }

            if (call.Args.Count > 1 && GetInfixOpOrNull(call.FunctionName) is Op op) {
                return new InfixOperation(op, call.Args.Select(Convert).ToList());
            }

            throw new KeyNotFoundException($"Expression includes unmapped SMT function \"{call.FunctionName}\"");
        }

        private static UnaryOp? GetUnaryOpOrNull(string name) => name switch {
            "not" => UnaryOp.Not,
            "-" => UnaryOp.Minus,
            _ => null,
        };

        private static Op? GetInfixOpOrNull(string smtFn) => smtFn switch {
            "=" => Op.Eq,
            "!=" => Op.Neq,
            "and" => Op.And,
            "or" => Op.Or,
            "+" => Op.Plus,
            "-" => Op.Minus,
            "*" => Op.Times,
            "<" => Op.Lt,
            ">" => Op.Gt,
            "<=" => Op.Leq,
            ">=" => Op.Geq,
            _ => null,
        };
    }

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
        IReadOnlyList<MonoQueryBundle> QueryFunctions { get; } // at most one per sem

        IReadOnlyDictionary<Identifier, (int prod_idx, int sem_idx)> QueryIdMap { get; }

        public MonotonicityStep(InitialStuff stuff) {
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
                        query_fns.Add(bundle);
                        query_fn_id_map.Add(bundle.output_transformer.Id, (i, j));
                    }
                }
            }

            StructDefs = struct_defs;
            QueryFunctions = query_fns;
            QueryIdMap = query_fn_id_map;
        }


        public IEnumerable<IStatement> GetFile() {
            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            foreach (var st in StructDefs) {
                yield return st.GetStructDef();
                yield return st.GetEqualityFunction();
                yield return st.GetCompareGenerator();
                yield return st.GetDisjunctGenerator();
                yield return st.GetNonEqualityHarness();
            }

            foreach (var q in QueryFunctions) {
                foreach (var fdef in q.preconditions.Append(q.output_transformer)) yield return fdef;
            }

            yield return GetMain(StructDefs, QueryFunctions);
        }

        /*
        static FunctionDefinition GetMinimizerHarness(Variable mono_count, int max_mono) {
            return new(new(FunctionModifier.Harness, VoidType.Id, new Identifier("maximize_mono")),
                new MinimizeStatement(Op.Minus.Of(new Literal(max_mono), mono_count.Ref()))
            );
        }

        static IEnumerable<FunctionDefinition> GetMonotonicityHarnesses(FunctionDefinition f, IReadOnlyList<int> relevant_block_ids, IReadOnlyDictionary<Identifier, StructType> struct_types) {
            Debug.Assert(f.Signature.ReturnTypeId == BitType.Id);
            Debug.Assert(f.Signature.Args.Count > 1);
            Debug.Assert(f.Signature.Args.Select((a, i) => (a, i)).All(t => t.a.IsRef == (t.i == 1)));
            Debug.Assert(!relevant_block_ids.Contains(1));

            foreach (var i in relevant_block_ids) {
                yield return GetMonotonicityHarness(f, i, struct_types);
            }
        }
        static FunctionDefinition GetMonotonicityHarness(FunctionDefinition f, int target_idx, IReadOnlyDictionary<Identifier, StructType> struct_types) {
            var n = f.Signature.Args.Count;

            var raw_outer_args = new List<FunctionArg>();
            var steps = new List<IStatement>();

            var output_st = struct_types[f.Signature.Args[1].TypeId];

            var constructed_vars = new List<Variable?>();

            for (int i = 0; i < n; i++) {
                if (i == 1) {
                    constructed_vars.Add(null);
                } else {
                    var arg_var = f.Signature.Args[i].Variable;
                    struct_types[arg_var.TypeId].PutConstructionForInputBlock(raw_outer_args, steps, arg_var);
                    constructed_vars.Add(arg_var);
                }
            }


            var target_st = struct_types[f.Signature.Args[target_idx].TypeId];
            var alt = new Variable("alt", target_st.Id);
            struct_types[f.Signature.Args[target_idx].TypeId].PutConstructionForInputBlock(raw_outer_args, steps, alt);

            var mono_kind = new Variable("mono_kind", IntType.Id);
            steps.Add(mono_kind.Declare(new Hole(2,$"#MONO! {f.Id}.{target_idx}")));

            var a_out = new Variable("a_out", output_st.Id);
            var b_out = new Variable("b_out", output_st.Id);

            steps.Add(a_out.Declare());
            steps.Add(b_out.Declare());

            var arg_list_a = new IExpression[n];
            var arg_list_b = new IExpression[n];

            for (int i = 0; i < n; i++) {
                if (i == 1) {
                    arg_list_a[i] = a_out.Ref();
                    arg_list_b[i] = b_out.Ref();
                } else {
                    arg_list_a[i] = constructed_vars[i]!.Ref();
                    if (i == target_idx) {
                        arg_list_b[i] = alt.Ref();
                    } else {
                        arg_list_b[i] = constructed_vars[i]!.Ref();

                    }
                }
            }


            // If !cmp(alt, arg_i) return early
            steps.Add(new IfStatement(
                Op.And.Of(
                    target_st.CompareId.Call(alt, constructed_vars[target_idx]!),
                    f.Call(arg_list_a),
                    f.Call(arg_list_b)
                ), mono_kind.IfEq(Lit0,
                    output_st.EqId.Call(a_out, b_out).Assert()
                ).ElseIf(Op.Eq.Of(mono_kind.Ref(), Lit1),
                    output_st.CompareId.Call(a_out, b_out).Assert()
                ).ElseIf(Op.Eq.Of(mono_kind.Ref(), Lit2),
                    output_st.CompareId.Call(b_out, a_out).Assert()
                )
            ));
            steps.Add(new MinimizeStatement(mono_kind.Ref()));


            return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"mono_{f.Id}_v{target_idx}"), raw_outer_args), steps);
        }*/


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
                        mono = new Monotonicity[fdef.Signature.Args.Count+1]; // include the output slot as having constant monotonicity

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
            IReadOnlyList<MonoQueryBundle> query_subjects
        ) {
            var instances_needed_per_type = new Dictionary<Identifier, int>();

            static int map_block_id(int i) => i > 1 ? i - 1 : i;

            // at least 3 instances are needed to check transitivity
            foreach (var t_t in tuple_defs) {
                instances_needed_per_type.Add(t_t.Id, 3);
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

            var ns = new FunctionNamespace2(pre_list);

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
                // temp disabled
                //steps.Add(new IfStatement(ns.Convert(assert.Expression), new ReturnStatement(new Literal(0))));
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

                //steps.Add(args[assign.TargetBlockId].Assign(ret));
                steps.Add(new ReturnStatement(ret));
            }
            void assign_output_from_block(int src_id) {
                if (transformer_req_block_ids is not null) throw new InvalidOperationException();
                transformer_req_block_ids = new();

                //steps.Add(args[1].Assign(args[src_id]));
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

            // return true if reached end
            //steps.Add(new ReturnStatement(new Literal(1)));

            Debug.Assert(transformer_req_block_ids is not null);
            var tf = new FunctionDefinition(new(FunctionModifier.None, /*BitType.Id*/ args[1].TypeId, fn_id, args.SkipAt(1).ToList()), steps);
            return new(tf, preconditions, transformer_req_block_ids);
        }


        /*
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyDictionary<Identifier, StructType> StructTypeMap { get; }
        private IReadOnlyList<FunctionDefinition> MaybeMonotoneFunctions { get; }
        private IReadOnlyList<FunctionDefinition> OtherFunctions { get; }
        private IReadOnlyList<MonotoneLabeling> ConstantTransformers { get; }
        public IReadOnlyList<Identifier> OrderedFunctionIds { get; }

        public MonotonicityStep(
            IReadOnlyList<StructType> structs,
            IReadOnlyList<FunctionDefinition> maybeMonotoneFunctions,
            IReadOnlyList<FunctionDefinition> otherFunctions,
            IReadOnlyList<MonotoneLabeling> constantTransformers,
            IReadOnlyList<Identifier> orderedFunctionIds
        ) {
            Structs = structs;
            StructTypeMap = structs.ToDictionary(s => s.Id);
            MaybeMonotoneFunctions = maybeMonotoneFunctions;
            OtherFunctions = otherFunctions;
            ConstantTransformers = constantTransformers;
            OrderedFunctionIds = orderedFunctionIds;
        }

        //static string SmtArgListString(IEnumerable<Operational.VariableInfo> args) => string.Join(" ", args.Select(a => $"({a.Sort.Name} {a.Name})"));

        public IEnumerable<IStatement> GetFile() {
            foreach (var st in Structs) {
                yield return st.GetStructDef();
            }
            foreach (var st in Structs) {
                yield return st.GetEqualityFunction();
                yield return st.GetCompareGenerator();
                yield return st.GetDisjunctGenerator();
            }

            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            foreach (var fn in OtherFunctions) {
                yield return fn;
            }
            foreach (var fn in MaybeMonotoneFunctions) {
                yield return fn;
            }

            foreach (var st in Structs) {
                yield return st.GetNonEqualityHarness();
            }

            yield return GetMain();
        }

        public FunctionDefinition GetMain() {
            var clasps = Clasp.GetAll(Structs, StructTypeMap, MaybeMonotoneFunctions.Select(f => f.Signature));

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = GetMainInitContent(clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList());

            body.AddRange(input_assembly_statements);

            body.Add(new Annotation("Check partial equality properties", 2));
            foreach (var c in clasps) {
                body.AddRange(c.Type.GetPartialEqAssertions(c.Indexed[0].Sig(), c.Indexed[1].Sig(), c.Indexed[2].Sig()));
            }

            body.Add(new Annotation("Monotonicity", 2));

            var n_mono = new Variable("n_mono", IntType.Id);
            body.Add(new VariableDeclaration(n_mono, Lit0));

            var claspMap = clasps.ToDictionary(v => v.Type.Id);
            foreach (var fn in MaybeMonotoneFunctions) {
                body.AddRange(GetMonoAssertions(n_mono, claspMap, fn));
            }

            int n_mono_checks = MaybeMonotoneFunctions.Sum(fn => fn.Signature.Args.Count);

            body.Add(new MinimizeStatement(Op.Minus.Of(new Literal(n_mono_checks), n_mono.Ref())));

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("main"), input_args), body);
        }
        */
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
                yield return mono_flag.Declare(new Hole(2,$"#MONO {query.output_transformer.Id}.{target_idx_raw}"));

                var target_idx = map_block_id(target_idx_raw);

                var args = sig.Args;
                var n = args.Count;

                var target_st = types[args[target_idx].TypeId]; //types[args[target_idx].TypeId];
                var output_st = types[sig.ReturnTypeId]; //types[args[1].TypeId];



                //var a_out = new Variable($"{subject.Id}_out_{target_idx}_a", output_st.Id);
                //var b_out = new Variable($"{subject.Id}_out_{target_idx}_b", output_st.Id);

                //yield return a_out.Declare();
                //yield return b_out.Declare();

                var arg_list_a = new IExpression[n];
                var arg_list_b = new IExpression[n];

                var ctr = new Counter<Identifier>();

                IExpression a_in = null, b_in = null;

                for (int i = 0; i < n; i++) {
                    //if (i == 1) {
                    //    arg_list_a[i] = a_out.Ref();
                    //    arg_list_b[i] = b_out.Ref();
                    //} else {
                        var u = args[i].TypeId;

                        arg_list_a[i] = instances[u][ctr.Increment(u) - 1].Ref();
                        if (i == target_idx) {
                            arg_list_b[i] = instances[u][ctr.Increment(u) - 1].Ref();
                            a_in = arg_list_a[i];
                            b_in = arg_list_b[i];
                        } else {
                            arg_list_b[i] = arg_list_a[i];
                        }
                    //}
                }

                Debug.Assert(a_in is not null && b_in is not null);

                IExpression inputs_ord = target_st.CompareId.Call(a_in, b_in);
                var guard = query.preconditions.Count == 0 ? inputs_ord :
                    Op.And.Of(query.preconditions.SelectMany(p => new IExpression[] { p.Call(a_in), p.Call(b_in) }).Append(inputs_ord).ToList());

                var a_out = query.output_transformer.Call(arg_list_a);
                var b_out = query.output_transformer.Call(arg_list_b);

                    //Op.And.Of(
                    //    target_st.CompareId.Call(a_in, b_in),
                    //    subject.Call(arg_list_a),
                    //    subject.Call(arg_list_b)
                    //);


                yield return mono_flag.IfEq(Lit0,
                    //Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(a_out, b_out)).Assert()
                    Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(a_out,b_out)).Assert()
                ).ElseIf(Op.Eq.Of(mono_flag.Ref(), Lit1),
                    Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(b_out,a_out)).Assert()
                //Op.Or.Of(UnaryOp.Not.Of(guard), output_st.CompareId.Call(b_out, a_out)).Assert()
                ).Else(
                    cost.Assign(Op.Plus.Of(cost.Ref(), Lit1))
                );

                //yield return mono_flag.IfEq(Lit0,
                //    new IfStatement(guard, output_st.EqId.Call(a_out, b_out).Assert())
                //).ElseIf(Op.Eq.Of(mono_flag.Ref(), Lit1),
                //    cost.Assign(Op.Plus.Of(cost.Ref(), Lit1)),
                //        new IfStatement(guard, output_st.CompareId.Call(a_out, b_out).Assert())
                //).ElseIf(Op.Eq.Of(mono_flag.Ref(), Lit2),
                //    cost.Assign(Op.Plus.Of(cost.Ref(), Lit1)),
                //    new IfStatement(guard, output_st.CompareId.Call(b_out, a_out).Assert())
                //).Else(
                //    cost.Assign(Op.Plus.Of(cost.Ref(), Lit2))
                //);
            }
        }

        /*
         * public static (IReadOnlyList<FunctionArg> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<RichTypedVariable> input_structs) {
            List<FunctionArg> input_args = new();
            List<IStatement> input_assembly_statements = new();

            input_assembly_statements.Add(new Annotation("Assemble structs"));

            foreach (var obj in input_structs) {
                if (obj.Type is not StructType st) throw new NotSupportedException();
                List<FunctionArg> locals = new();
                foreach (var prop in st.Elements) {
                    //if (prop.Type is StructType) throw new NotSupportedException();
                    locals.Add(new FunctionArg(new($"{obj.Id}_{prop.Id}"), prop.TypeId));
                }

                input_args.AddRange(locals);
                input_assembly_statements.Add(new VariableDeclaration(obj.Sig(), st.New(st.Elements.Select((prop, i) => prop.Assign(locals[i].Ref())))));
            }

            return (input_args, input_assembly_statements);
        }
        */
        public record Output(IReadOnlyList<StructType> StructDefs, IReadOnlyList<FunctionDefinition> Comparisons, IReadOnlyList<AnnotatedQueryFunction> QueryFunctions) {
            public IEnumerable<(StructType st, FunctionDefinition cmp)> ZipComparisonsToTypes() {

                var a = Comparisons.ToDictionary(c => c.Id);

                return StructDefs.Select(st => (st, a[st.CompareId]));
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

            //await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_cmp.PathWsl);

            Console.WriteLine($"--- [Initial] Reading compare functions ---");

            var compare_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), this.StructDefs.Select(s => s.CompareId));

            Debug.Assert(compare_functions.Count == this.StructDefs.Count, "Failed to extract all comparison functions; halting");

            Console.WriteLine($"--- [Initial] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted = PipelineUtil.ReduceEachToSingleExpression(compare_functions); // May throw

            return new(StructDefs, compacted, annotated);
        }

        //private static IReadOnlyDc<MonotoneLabeling> Sequence(IReadOnlyList<Identifier> indices, IEnumerable<MonotoneLabeling> items) {
        //    var dict = items.ToDictionary(q => q.Function.Id);
        //    return indices.Select(i => dict[i]).ToList();
        //}

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
