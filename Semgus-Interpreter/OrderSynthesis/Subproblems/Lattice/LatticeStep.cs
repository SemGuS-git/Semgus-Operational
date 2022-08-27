using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.OrderSynthesis.Subproblems.LatticeSubstep;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.Subproblems {
    internal interface IRichTupleDescriptor { 
        StructType type { get; }
    }
    internal record NonLatticeDefs(StructType type) : IRichTupleDescriptor;

    internal record LatticeDefs(StructType type, FunctionDefinition compare, FunctionDefinition top, FunctionDefinition bot, FunctionDefinition join_incomparable, FunctionDefinition meet_incomparable) : IRichTupleDescriptor {
        internal IEnumerable<FunctionDefinition> GetEach() {
            yield return top;
            yield return bot;
            yield return join_incomparable;
            yield return meet_incomparable;
        }
    }

    internal class LatticeStep {
        public List<(StructType type, FunctionDefinition? compare)> Targets { get; }

        public LatticeStep(IEnumerable<(StructType type, FunctionDefinition? compare)> targets) {
            this.Targets = targets.ToList();
        }

        public record Output(IReadOnlyList<IRichTupleDescriptor> Lattices);

        private static async Task<LatticeDefs> DoOne(FlexPath dir, StructType type, FunctionDefinition compare, bool skip_refine) {

            var gen_top = new TopOrBot(true, type, compare);
            var gen_bot = new TopOrBot(false, type, compare);
            var gen_join = new JoinOrMeet(true, type, compare);
            var gen_meet = new JoinOrMeet(false, type, compare);

            Dictionary<Identifier, FunctionDefinition> final = new();

            foreach (var generator in new ILatticeSubstep[] { gen_top, gen_bot, gen_join, gen_meet }) {
                System.Console.WriteLine($"--- [Lattice] doing {generator.SynthFunId} for {type.Id} ---");

                var base_path = dir / $"{generator.SynthFunId}";

                var content = generator.GetInitialFile();

                var current_zone = base_path / "_init/";

                FunctionDefinition? synth_item = null;

                const int MAX_ITER = 100;
                for (int i = 0; i < (skip_refine ? 1 : MAX_ITER); i++) {
                    Directory.CreateDirectory(current_zone.Value);

                    var file_in = current_zone / "input.sk";
                    var file_out = current_zone / "result.sk";
                    var file_xml = current_zone / "holes.xml";
                    PipelineUtil.WriteSketchFile(file_in, content);

                    var (sketch_ok, sketch_out) = await IpcUtil.RunSketch(current_zone, "input.sk", "result.holes.xml");

                    _ = Task.Run(() => File.WriteAllText(file_out.Value, sketch_out));

                    if (!sketch_ok) {
                        break;
                    }

                    synth_item = ReadTarget(sketch_out, generator.SynthFunId);
                    content = generator.GetRefinementFile(synth_item.RenamedTo(new("prev_" + synth_item.Id)));
                    current_zone = base_path / $"_{i}/";
                }

                if (synth_item is null) {
                    throw new Exception("Failed to complete initial sketch step");
                } else {
                    final.Add(generator.SynthFunId, PipelineUtil.ReduceToSingleExpression(synth_item));
                }
            }

            return new(
                type,
                compare,
                final[gen_top.SynthFunId],
                final[gen_bot.SynthFunId],
                final[gen_join.SynthFunId],
                final[gen_meet.SynthFunId]
            );
        }




        static FunctionDefinition ReadTarget(string text, Identifier targetId) {
            var result_fn = PipelineUtil.ReadSelectedFunctions(text, new[] { targetId }).Single();
            return PipelineUtil.SloppyFunctionalize(result_fn);
        }


        public async Task<Output> Execute(FlexPath dir, bool skip_refine) {
            Directory.CreateDirectory(dir.Value);

            System.Console.WriteLine($"--- [Lattice] starting ---");

            List<IRichTupleDescriptor> output = new();

            foreach (var (type, compare) in Targets) {
                if (compare is null) {
                    output.Add(new NonLatticeDefs(type));
                } else {
                    var result = await DoOne(dir / $"{type.Id}/", type, compare, skip_refine);
                    output.Add(result);

                    PipelineUtil.WriteSketchFile(dir / $"{type.Id}.lattice.sk", result.GetEach());
                }
            }

            System.Console.WriteLine($"--- [Lattice] done ---");


            return new(output);
        }
    }
}
