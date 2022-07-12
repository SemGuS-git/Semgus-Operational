using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class SimplificationStep {
        //public record Config(
        //    IReadOnlyDictionary<Identifier, StructType> StructTypeMap,
        //    IReadOnlyList<StructType> StructTypeList,
        //    IReadOnlyList<FunctionDefinition> PrevComparisons
        //);

        private record SimplificationClasp(StructType Type, RichTypedVariable A, RichTypedVariable B);

        //public IReadOnlyDictionary<Identifier, StructType> StructTypeMap { get; }
        //public IReadOnlyList<StructType> StructTypeList { get; }
        //public IReadOnlyList<FunctionDefinition> PrevComparisons { get; }
        //public IReadOnlyList<Variable> Budgets { get; private set; }
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyDictionary<Identifier, FunctionDefinition> PrevComparisonsByStId { get; }

        public SimplificationStep(MonotonicityStep.Output prior) {
            var compare_to_st_map = prior.StructDefs.ToDictionary(st => st.CompareId, st => st.Id);

            //Iter = iter;
            //Structs = struct_types;

            PrevComparisonsByStId = prior.Comparisons
                .Select(c => (compare_to_st_map[c.Id], c with { Signature = c.Signature with { Id = new($"prev_{c.Id}") } }))
                .ToDictionary(t => t.Item1, t => t.Item2);
            Structs = prior.StructDefs;

            //Budgets = StructTypeList.Select(s => new Variable($"budget_{s.Id}", IntType.Id)).ToList();

            //Debug.Assert(PrevComparisons.All(f => StructTypeMap.TryGetValue(f.Signature.Args[0].TypeId, out var st) && f.Signature.Id != st.CompareId));
        }

        private record Bundle(StructType st, FunctionDefinition prev_cmp, Variable budget);

        public IEnumerable<IStatement> GetFile() {
            var bundles = Structs.Select(s => new Bundle(s, PrevComparisonsByStId[s.Id], new Variable($"budget_{s.Id}", IntType.Id))).ToList();


            foreach (var b in bundles) {
                var st = b.st;
                yield return b.budget.Declare(new Hole());
                yield return st.GetStructDef();
                yield return b.prev_cmp;
                yield return st.GetCompareReductionGenerator(b.budget);
                yield return st.GetDisjunctGenerator();
                yield return st.GetCompareMatchHarness(b.prev_cmp.Id);
            }

            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            yield return GetMain(bundles);
        }
        static FunctionDefinition GetMain(IEnumerable<Bundle> bundles) {
            List<IStatement> body = new();

            body.Add(new MinimizeStatement(Op.Plus.Of(bundles.Select(b => b.budget.Ref()).ToList())));

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("reduce_complexity")), body);
        }

        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);

        public async Task<Output> Execute(FlexPath dir, bool reuse_prev = false) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            //var file_cmp = dir.Append("result.comparisons.sk");

            if (reuse_prev) {
                System.Console.WriteLine($"--- [Reduction] Reusing prev ---");
                if (!File.Exists(file_out.PathWin)) {
                    System.Console.WriteLine($"--- [Reduction] No prev result (throw) ---");
                    throw new Exception("Oof");
                }
            } else {
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
            }

            Console.WriteLine($"--- [Reduction] Reading compare functions ---");

            var compare_ids = this.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisonsByStId.Values.Select(p => p.Id));
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
                compacted = PipelineUtil.ReduceEachToSingleExpression(to_compact, to_reference);
            } catch (Exception) {
                Console.WriteLine($"--- [Reduction] Failed to transform all comparison functions; halting ---");
                throw;
            }

            return new(compacted);
        }
    }
}
