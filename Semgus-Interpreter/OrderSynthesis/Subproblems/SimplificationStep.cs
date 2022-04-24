using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class SimplificationStep {
        public record Config(
            IReadOnlyDictionary<Identifier, StructType> StructTypeMap,
            IReadOnlyList<StructType> StructTypeList,
            IReadOnlyList<FunctionDefinition> PrevComparisons
        );

        private record SimplificationClasp(StructType Type, Variable A, Variable B);

        public IReadOnlyDictionary<Identifier, StructType> StructTypeMap { get; }
        public IReadOnlyList<StructType> StructTypeList { get; }
        public IReadOnlyList<FunctionDefinition> PrevComparisons { get; }
        public IReadOnlyList<Variable> Budgets { get; private set; }

        public SimplificationStep(Config config) {

            (StructTypeMap, StructTypeList, PrevComparisons) = config;

            Budgets = StructTypeList.Select(s => new Variable("budget_" + s.Name, IntType.Instance)).ToList();

            Debug.Assert(PrevComparisons.All(f => StructTypeMap.TryGetValue(f.Signature.Args[0].TypeId, out var st) && f.Signature.Id != st.CompareId));
        }


        public IEnumerable<IStatement> GetFile() {
            foreach (var b in Budgets) {
                yield return b.Declare(new Hole());
            }

            foreach (var st in StructTypeList) {
                yield return st.GetStructDef();
            }
            for (int i = 0; i < StructTypeList.Count; i++) {
                StructType? st = StructTypeList[i];
                yield return PrevComparisons[i];
                yield return st.GetCompareReductionGenerator(Budgets[i]);
                yield return st.GetDisjunctGenerator();
            }

            yield return BitType.GetAtom();
            yield return IntType.GetAtom();

            yield return GetMain();
        }


        public FunctionDefinition GetMain() {
            var clasps = StructTypeList.Select(s => new SimplificationClasp(s, new Variable(s.Name + "_s0", s), new Variable(s.Name + "_s1", s))).ToList();

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = MonotonicityStep.GetMainInitContent(clasps.SelectMany(c => new[] { c.A, c.B }).ToList());

            body.AddRange(input_assembly_statements);

            foreach (var (clasp, prev) in clasps.Zip(PrevComparisons)) {
                body.Add(new AssertStatement(
                    Op.Eq.Of(
                        clasp.Type.CompareId.Call(clasp.A.Ref(), clasp.B.Ref()),
                        prev.Call(clasp.A.Ref(), clasp.B.Ref())
                        )
                 ));
            }

            body.Add(new MinimizeStatement(Op.Plus.Of(Budgets.Select(b => b.Ref()).ToList())));

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Instance, new("main"), input_args), body);
        }

        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);

        public async Task<Output> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            //var file_cmp = dir.Append("result.comparisons.sk");

            System.Console.WriteLine($"--- [Reduction] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Reduction] Invoking Sketch on {file_in} ---");

            var step2_sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (step2_sketch_result) {
                Console.WriteLine($"--- [Reduction] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Reduction] Sketch rejected; reduction failed ---");
                throw new Exception("Sketch rejected");
            }

            Console.WriteLine($"--- [Reduction] Reading compare functions ---");

            var compare_ids = this.StructTypeList.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisons.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), extraction_targets);
            } catch (Exception) {
                Console.WriteLine($"--- [Reduction] Failed to extract all comparison functions ---");
                throw;
            }

            Console.WriteLine($"--- [Reduction] Transforming compare functions ---");

            var (to_compact, to_reference) = extracted_functions.GetEnumerator().Partition(f => compare_ids.Contains(f.Id)).ReadToLists();

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = PipelineUtil.ReduceEachToSingleExpression(to_compact,to_reference);
            } catch (Exception) {
                Console.WriteLine($"--- [Reduction] Failed to transform all comparison functions; halting ---");
                throw;
            }

            return new(compacted);
        }
    }
}
