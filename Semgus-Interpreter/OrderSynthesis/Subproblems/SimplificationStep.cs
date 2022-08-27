using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class SimplificationStep {
        public IReadOnlyList<StructType> StructsToOrder { get; }
        public IReadOnlyDictionary<Identifier, FunctionDefinition> PrevComparisonsByStId { get; }

        public SimplificationStep(MonotonicityStep.Output prior) {
            StructsToOrder = prior.StructDefsToOrder;

            var compare_to_st_map = StructsToOrder.ToDictionary(st => st.CompareId, st => st.Id);

            PrevComparisonsByStId = prior.Comparisons
                .Select(c => (compare_to_st_map[c.Id], c with { Signature = c.Signature with { Id = new($"prev_{c.Id}") } }))
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private record Bundle(StructType st, FunctionDefinition prev_cmp, Variable budget);

        public IEnumerable<IStatement> GetFile() {
            var bundles = StructsToOrder.Select(s => new Bundle(s, PrevComparisonsByStId[s.Id], new Variable($"budget_{s.Id}", IntType.Id))).ToList();


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

        public async Task<Output> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.Value);

            var file_in = dir / "input.sk";
            var file_out = dir / "result.sk";
            var file_holes = dir / "result.holes.xml";
            //var file_cmp = dir / "result.comparisons.sk";

            System.Console.WriteLine($"--- [Reduction] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.Value)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Reduction] Invoking Sketch on {file_in} ---");

            var (sketch_ok, sketch_out) = await IpcUtil.RunSketch(dir, "input.sk", "result.holes.xml");

            _ = Task.Run(() => File.WriteAllText(file_out.Value, sketch_out));

            if (sketch_ok) {
                Console.WriteLine($"--- [Reduction] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Reduction] Sketch rejected; reduction failed ---");
                throw new Exception("Sketch rejected");
            }

            Console.WriteLine($"--- [Reduction] Reading compare functions ---");

            var compare_ids = this.StructsToOrder.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisonsByStId.Values.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = PipelineUtil.ReadSelectedFunctions(sketch_out, extraction_targets);
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
