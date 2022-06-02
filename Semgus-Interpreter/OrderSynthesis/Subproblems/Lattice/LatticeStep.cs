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
    internal record LatticeDefs(StructType type, FunctionDefinition compare, FunctionDefinition top, FunctionDefinition bot, FunctionDefinition join_incomparable, FunctionDefinition meet_incomparable) {
        internal IEnumerable<FunctionDefinition> GetEach() {
            yield return top;
            yield return bot;
            yield return join_incomparable;
            yield return meet_incomparable;
        }
    }

    internal class LatticeStep {
        public LatticeStep(IEnumerable<(StructType type, FunctionDefinition compare)> targets) {
            this.Targets = targets.ToList();
        }
        public List<(StructType type, FunctionDefinition compare)> Targets { get; }

        public record Output(IReadOnlyList<LatticeDefs> Lattices);

        private static async Task<LatticeDefs> DoOne(FlexPath dir, StructType type, FunctionDefinition compare, bool skip_refine) {

            var gen_top = new TopOrBot(true, type, compare);
            var gen_bot = new TopOrBot(false, type, compare);
            var gen_join = new JoinOrMeet(true, type, compare);
            var gen_meet = new JoinOrMeet(false, type, compare);

            Dictionary<Identifier, FunctionDefinition> final = new();

            foreach (var generator in new ILatticeSubstep[] { gen_top, gen_bot, gen_join, gen_meet }) {
                System.Console.WriteLine($"--- [Lattice] doing {generator.SynthFunId} for {type.Id} ---");

                var base_path = dir.Append($"{generator.SynthFunId}");

                var content = generator.GetInitialFile();

                var current_zone = base_path.Append("_init/");

                FunctionDefinition? synth_item = null;

                const int MAX_ITER = 100;
                for (int i = 0; i < (skip_refine ? 1 : MAX_ITER); i++) {
                    Directory.CreateDirectory(current_zone.PathWin);

                    var file_in = current_zone.Append("input.sk");
                    var file_out = current_zone.Append("result.sk");
                    var file_xml = current_zone.Append("holes.xml");
                    PipelineUtil.WriteSketchFile(file_in, content);

                    if (!(await Wsl.RunSketch(file_in, file_out, file_xml))) {
                        break;
                    }

                    synth_item = await ReadTarget(file_out, generator.SynthFunId);
                    content = generator.GetRefinementFile(synth_item.RenamedTo(new("prev_" + synth_item.Id)));
                    current_zone = base_path.Append($"_{i}/");
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




        static async Task<FunctionDefinition> ReadTarget(FlexPath path, Identifier targetId) {
            var result_fn = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(path.PathWin), new[] { targetId }).Single();
            return PipelineUtil.SloppyFunctionalize(result_fn);
        }


        public async Task<Output> Execute(FlexPath dir, bool skip_refine) {
            Directory.CreateDirectory(dir.PathWin);

            System.Console.WriteLine($"--- [Lattice] starting ---");

            List<LatticeDefs> output = new();

            foreach (var (type, compare) in Targets) {
                var result = await DoOne(dir.Append($"{type.Id}/"), type, compare, skip_refine);
                output.Add(result);

                PipelineUtil.WriteSketchFile(dir.Append($"{type.Id}.lattice.sk"), result.GetEach());
            }

            System.Console.WriteLine($"--- [Lattice] done ---");


            return new(output);
        }
    }
}
