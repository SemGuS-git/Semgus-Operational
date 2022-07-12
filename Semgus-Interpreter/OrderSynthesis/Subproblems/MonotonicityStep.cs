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
    class FunctionNamespace2 {
        public Dictionary<BlockItemRef, ISettable> VarMap { get; } = new();

        public FunctionNamespace2(IEnumerable<(FunctionArg, StructType)> args) {
            var var_map = new Dictionary<BlockItemRef, ISettable>();

            int i = 0;
            foreach (var arg in args) {
                for (int j = 0; j < arg.Item2.Elements.Count; j++) {
                    var item = arg.Item2.Elements[j];
                    VarMap.Add(new(i, j), arg.Item1.Variable.Get(item.Id));
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
        IReadOnlyList<(FunctionDefinition fdef, List<int> relevant_block_ids)> QueryFunctions { get; } // at most one per sem

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

            List<(FunctionDefinition fdef, List<int> relevant_block_ids)> query_fns = new();

            for (int i = 0; i < stuff.Productions.Count; i++) {
                var prod = stuff.Productions[i];
                for (int j = 0; j < prod.Semantics.Count; j++) {
                    var sem = prod.Semantics[j];
                    var fn_id = new Identifier($"prod_{i}_sem_{j}");
                    var (fdef, req_block_ids) = GetMonoSubjectFunction(struct_defs, fn_id, sem);

                    // omit sems with only trivial transformers.
                    // - If directly passing output from a child term, no transformer step is required.
                    // - If assigning from variable-free formulas, e.g. in literal terminals, we implicitly construct a singular interval.

                    if (req_block_ids.Count>0) {
                        query_fns.Add((fdef, req_block_ids));
                        query_fn_id_map.Add(fdef.Id, (i, j));
                    }
                }
            }

            StructDefs = struct_defs;
            QueryFunctions = query_fns;
            QueryIdMap = query_fn_id_map;
        }


        public IEnumerable<IStatement> GetFile() {
            var mono_count = new Variable("mono_count", IntType.Id);
            yield return mono_count.Declare(new Hole());

            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            foreach (var st in StructDefs) {
                yield return st.GetStructDef();
                yield return st.GetEqualityFunction();
                yield return st.GetCompareGenerator();
                yield return st.GetDisjunctGenerator();
                yield return st.GetPartialOrderHarness();
                yield return st.GetNonEqualityHarness();
            }

            var sd_dict = StructDefs.ToDictionary(a => a.Id);


            var max_mono = 0;
            foreach (var q in QueryFunctions) {
                yield return q.fdef;

                foreach (var harness in GetMonotonicityHarnesses(q.fdef, q.relevant_block_ids, sd_dict, mono_count)) {
                    max_mono += 2;
                    yield return harness;
                }
            }

            yield return GetMinimizerHarness(mono_count, max_mono);
        }

        static FunctionDefinition GetMinimizerHarness(Variable mono_count, int max_mono) {
            return new(new(FunctionModifier.Harness, VoidType.Id, new Identifier("maximize_mono")),
                new MinimizeStatement(Op.Minus.Of(new Literal(max_mono), mono_count.Ref()))
            );
        }

        static IEnumerable<FunctionDefinition> GetMonotonicityHarnesses(FunctionDefinition f, IReadOnlyList<int> relevant_block_ids, IReadOnlyDictionary<Identifier, StructType> struct_types, Variable mono_count) {
            Debug.Assert(f.Signature.ReturnTypeId == BitType.Id);
            Debug.Assert(f.Signature.Args.Count > 1);
            Debug.Assert(f.Signature.Args.Select((a, i) => (a, i)).All(t => t.a.IsRef == (t.i == 1)));

            foreach (var i in relevant_block_ids) {
                if (i == 1) throw new ArgumentException();

                yield return GetMonotonicityHarness(f, i, struct_types, mono_count);
            }
        }

        static FunctionDefinition GetMonotonicityHarness(FunctionDefinition f, int target_idx, IReadOnlyDictionary<Identifier, StructType> struct_types, Variable mono_count) {
            var n = f.Signature.Args.Count;

            var outer_args = new List<FunctionArg>();
            var steps = new List<IStatement>();

            var output_st = struct_types[f.Signature.Args[1].TypeId];

            for (int i = 0; i < n; i++) {
                if (i == 1) continue;
                var arg = f.Signature.Args[i];
                struct_types[arg.TypeId].PutConstructionForInputBlock(outer_args, steps, arg.Variable);
            }
            var target_st = struct_types[f.Signature.Args[target_idx].TypeId];
            var alt = new Variable("alt", target_st.Id);
            struct_types[f.Signature.Args[target_idx].TypeId].PutConstructionForInputBlock(outer_args, steps, alt);

            var flag_inc = new Variable("mono_inc", BitType.Id);
            var flag_dec = new Variable("mono_dec", BitType.Id);

            steps.Add(flag_inc.Declare(new Hole($"#MONO+ {f.Id}.{target_idx}")));
            steps.Add(flag_dec.Declare(new Hole($"#MONO- {f.Id}.{target_idx}")));


            // If !cmp(alt, arg_i) return early
            steps.Add(target_st.CompareId.Call(alt, outer_args[target_idx + 1].Variable).Assume());

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
                    arg_list_a[i] = outer_args[i].Ref();
                    if (i == target_idx) {
                        arg_list_b[i] = alt.Ref();
                    } else {
                        arg_list_b[i] = outer_args[i].Ref();

                    }
                }
            }

            // make sure the semantics hold
            steps.Add(f.Call(arg_list_a).Assume());
            steps.Add(f.Call(arg_list_b).Assume());

            steps.Add(new IfStatement(flag_inc.Ref(),
                new IfStatement(flag_dec.Ref(),
                    output_st.EqId.Call(a_out, b_out).Assert(),
                    mono_count.Assign(Op.Plus.Of(mono_count.Ref(), Lit2))
                ).Else(
                    output_st.CompareId.Call(a_out, b_out).Assert(),
                    mono_count.Assign(Op.Plus.Of(mono_count.Ref(), Lit1))
                )
              ).ElseIf(flag_dec.Ref(),
                output_st.CompareId.Call(b_out, a_out).Assert(),
                mono_count.Assign(Op.Plus.Of(mono_count.Ref(), Lit1))
              )
            );

            return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"mono_{f.Id}_v{target_idx}"), outer_args), steps);
        }

        public static void Au(IntervalSemantics.InitialStuff initial) {

            var struct_defs = initial.TypeHelper.BlockTypes.Select((bt, i) =>
                new StructType(
                    new($"block_{i}"),
                    bt.Members.Select((label, j) =>
                        new Variable($"v{j}", Util.MapLabelToTypeIdentifier(label))
                    ).ToList()
                )
            ).ToList();


            for (int i = 0; i < initial.Productions.Count; i++) {
                var prod = initial.Productions[i];
                for (int j = 0; j < prod.Semantics.Count; j++) {
                    var sem = prod.Semantics[j];

                    //sem.Steps
                }
            }
        }

        static IReadOnlyDictionary<int, (Identifier fn_id, int arg_id, bool is_inc_else_dec)> ScanHoleLines(StreamReader reader) {
            Regex a = new(@"/\*#MONO ([+-]) (.+)\.([0-9]+)\*/", RegexOptions.Compiled);
            Dictionary<int, (Identifier fn_id, int arg_id, bool is_inc_else_dec)> line_map = new();
            int i = 1;

            while(reader.ReadLine() is var line) { 
                var match = a.Match(line);
                if(match.Success) {
                    var is_inc_else_dec = match.Groups[1].Value switch {
                        "+" => true,
                        "-" => false,
                        _ => throw new InvalidDataException(),
                    };
                    var fn_id = new Identifier(match.Groups[2].Value);
                    var arg_id = int.Parse(match.Groups[3].Value);

                    line_map.Add(i, (fn_id, arg_id, is_inc_else_dec));
                }
                i++;
            }
            return line_map;
        }

        static IReadOnlyDictionary<Identifier,Monotonicity[]> ExtractMonotonicities(HolesXmlDoc doc, IReadOnlyDictionary<Identifier,FunctionDefinition> query_functions, IReadOnlyDictionary<int,(Identifier fn_id, int arg_id, bool is_inc_else_dec)> line_map) {
            HashSet<int> consumed_lines = new();

            var d_mono = new Dictionary<Identifier, Monotonicity[]>();

            foreach(var line in doc.Items) {
                if (line_map.TryGetValue (line.Line, out var target) ){
                    Debug.Assert(consumed_lines.Add(line.Line)); // Each relevant line should only have one hole

                    var fdef = query_functions[target.fn_id];

                    Monotonicity[] mono;
                    if (!d_mono.TryGetValue(target.fn_id, out mono)) {
                        mono = new Monotonicity[fdef.Signature.Args.Count - 1];

                        // Initialize values to constant.
                        // So, if a particular arg is not tested, we assume it is constant wrt its semantics.
                        Array.Fill(mono, Monotonicity.Constant);
                        d_mono.Add(target.fn_id, mono);
                    }


                    if(line.Value==0) {
                        mono[target.arg_id] = (target.is_inc_else_dec, mono[target.arg_id]) switch {
                            (true,Monotonicity.Constant) => Monotonicity.Decreasing,
                            (true,Monotonicity.Increasing) => Monotonicity.None,
                            (false,Monotonicity.Constant) => Monotonicity.Increasing,
                            (false,Monotonicity.Decreasing) => Monotonicity.None,
                            _ => throw new InvalidOperationException(),
                        };
                    }
                }
            }
            
            return d_mono;
        }

        static (FunctionDefinition fdef, List<int> transformer_req_block_ids) GetMonoSubjectFunction(IReadOnlyList<StructType> struct_defs, Identifier fn_id, BlockSemantics sem) {
            List<(FunctionArg, StructType)> pre_list = sem.BlockTypes.Select((bt, i) =>
                 (new FunctionArg(new($"b{i}"), struct_defs[bt].Id, i == 1), struct_defs[bt])
            ).ToList();

            var ns = new FunctionNamespace2(pre_list);

            var args = pre_list.Select(a => a.Item1).ToList();

            List<IStatement> steps = new();

            bool did_set_output = false;

            List<int> transformer_req_block_ids = null;

            StructType get_struct_def(int block_arg_idx) {
                return struct_defs[sem.BlockTypes[block_arg_idx]];
            }

            void assert_predicate(BlockAssert assert) {
                steps.Add(new IfStatement(ns.Convert(assert.Expression), new ReturnStatement(new Literal(0))));
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

                steps.Add(args[assign.TargetBlockId].Assign(ret));
            }
            void assign_output_from_block(int src_id) {
                if (transformer_req_block_ids is not null) throw new InvalidOperationException();
                transformer_req_block_ids = new();

                steps.Add(args[1].Assign(args[src_id]));
            }

            foreach (var step in sem.Steps) {
                switch (step) {
                    case BlockEval eval:
                        switch (eval.OutputBlockId) {
                            case 0:
                                throw new InvalidDataException();
                                break;
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
            steps.Add(new ReturnStatement(new Literal(1)));

            Debug.Assert(transformer_req_block_ids is not null);

            return (new(new(FunctionModifier.None, BitType.Id, fn_id, args), steps), transformer_req_block_ids);
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

        private IEnumerable<IStatement> GetMonoAssertions(Variable n_mono, IReadOnlyDictionary<Identifier, Clasp> clasps, FunctionDefinition fn) {
            var sig = fn.Signature;
            if (!StructTypeMap.TryGetValue(sig.ReturnTypeId, out var type_out)) throw new NotSupportedException();

            List<VariableRef> fixed_args = new();

            {
                Counter<Identifier> vcount = new();
                foreach (var v in sig.Args) {
                    var key = v.TypeId;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
                    vcount.Increment(key);
                }
            }

            yield return new Annotation($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < sig.Args.Count; i++) {
                if (!StructTypeMap.TryGetValue(sig.Args[i].TypeId, out var type_i)) throw new NotSupportedException();

                var alt_i = clasps[type_i.Id].Alternate;

                List<VariableRef> alt_args = new(fixed_args);
                alt_args[i] = alt_i.Ref();

                var mono_flag = new Variable($"mono_{fn.Id}_{i}", IntType.Id);

                yield return new VariableDeclaration(mono_flag, new Hole($"#MONO {fn.Id}_{i}"));
                yield return mono_flag.IfEq(Lit0,
                    Assertion(
                        type_i.CompareId.Call(fixed_args[i], alt_i.Ref())
                            .Implies(type_out.CompareId.Call(fn.Call(fixed_args), fn.Call(alt_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), Lit1))
                );
                yield return mono_flag.IfEq(Lit1,
                    Assertion(
                        type_i.CompareId.Call(fixed_args[i], alt_i.Ref())
                            .Implies(type_out.CompareId.Call(fn.Call(alt_args), fn.Call(fixed_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), Lit1))
                );
            }
        }

        public static (IReadOnlyList<FunctionArg> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<RichTypedVariable> input_structs) {
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
            public IEnumerable<(StructType st,FunctionDefinition cmp)> ZipComparisonsToTypes() {

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
                var f = qf.fdef;
                return new AnnotatedQueryFunction(f, QueryIdMap[f.Id],  qf.relevant_block_ids, mono[f.Id]);
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

            IReadOnlyDictionary<int, (Identifier fn_id, int arg_id, bool is_inc_else_dec)> line_map;

            using (var sr = new StreamReader(file_in.PathWin)) {
                line_map = ScanHoleLines(sr);
            }

            HolesXmlDoc doc;

            using (var fs = new FileStream(file_holes.PathWin, FileMode.Open)) {
                doc = (HolesXmlDoc)ser.Deserialize(fs)!;
            }

            return ExtractMonotonicities(doc, this.QueryFunctions.ToDictionary(a => a.fdef.Id, a=>a.fdef), line_map);
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
    internal record AnnotatedQueryFunction(FunctionDefinition fdef, (int prod_idx, int sem_idx) sem_addr, IReadOnlyList<int> relevant_block_ids, IReadOnlyList<Monotonicity> mono);

}
