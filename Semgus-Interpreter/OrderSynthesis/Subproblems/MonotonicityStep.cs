using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;

    internal class MonotonicityStep {
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
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons, IReadOnlyList<MonotoneLabeling> LabeledTransformers);

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

            var mono = await InspectMonotonicities(file_in, file_holes, file_mono);
            Debug.Assert(mono.Count == MaybeMonotoneFunctions.Count, "Missing monotonicity labels for some functions; halting");

            //await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_cmp.PathWsl);

            Console.WriteLine($"--- [Initial] Reading compare functions ---");

            var compare_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), this.Structs.Select(s => s.CompareId));

            Debug.Assert(compare_functions.Count == this.Structs.Count, "Failed to extract all comparison functions; halting");

            Console.WriteLine($"--- [Initial] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted = PipelineUtil.ReduceEachToSingleExpression(compare_functions); // May throw

            return new(compacted, Sequence(OrderedFunctionIds, mono.Concat(ConstantTransformers)));
        }

        private static IReadOnlyList<MonotoneLabeling> Sequence(IReadOnlyList<Identifier> indices, IEnumerable<MonotoneLabeling> items) {
            var dict = items.ToDictionary(q => q.Function.Id);
            return indices.Select(i => dict[i]).ToList();
        }

        private async Task<IReadOnlyList<MonotoneLabeling>> InspectMonotonicities(FlexPath file_in, FlexPath file_holes, FlexPath file_mono) {
            await Wsl.RunPython("read-mono-from-xml.py", file_in.PathWsl, file_holes.PathWsl, file_mono.PathWsl);
            return (await MonotoneLabeling.ExtractFromJson(this.MaybeMonotoneFunctions, file_mono.PathWin)).ToList();
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
}
