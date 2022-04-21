using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.Subproblems.LatticeSubstep;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record LatticeDefs(StructType type, FunctionDefinition top, FunctionDefinition bot, FunctionDefinition join, FunctionDefinition meet) {
        internal IEnumerable<FunctionDefinition> GetEach() {
            yield return top;
            yield return bot;
            yield return join;
            yield return meet;
        }
    }

    internal class LatticeStep {
        public LatticeStep(IEnumerable<(StructType type, FunctionDefinition compare)> targets) {
            this.Targets = targets.ToList();
        }
        public List<(StructType type, FunctionDefinition compare)> Targets { get; }

        public record Output(IReadOnlyList<LatticeDefs> Lattices);

        private static async Task<LatticeDefs> DoOne(FlexPath dir, StructType type, FunctionDefinition compare) {

            var gen_top = new TopOrBot(true, type, compare);
            var gen_bot = new TopOrBot(false, type, compare);
            var gen_join = new JoinOrMeet(true, type, compare);
            var gen_meet = new JoinOrMeet(false, type, compare);

            Dictionary<Identifier, FunctionDefinition> final = new();

            foreach (var generator in new ILatticeSubstep[] { gen_top, gen_bot, gen_join, gen_meet }) {
                System.Console.WriteLine($"--- [Lattice] doing {generator.TargetId} for {type.Id} ---");

                var base_path = dir.Append($"{generator.TargetId}");

                var content = generator.GetInitialFile();

                var current_zone = base_path.Append("_init/");

                FunctionDefinition? current = null;

                const int MAX_ITER = 100;
                for (int i = 0; i < MAX_ITER; i++) {
                    Directory.CreateDirectory(current_zone.PathWin);

                    var file_in = current_zone.Append("input.sk");
                    var file_out = current_zone.Append("result.sk");
                    var file_xml = current_zone.Append("holes.xml");
                    PipelineUtil.WriteSketchFile(file_in, content);

                    if (!(await Wsl.RunSketch(file_in, file_out, file_xml))) {
                        break;
                    }

                    current = await ReadTarget(file_out, generator.TargetId);
                    content = generator.GetRefinementFile(current);
                    current_zone = base_path.Append($"_{i}/");
                }

                if (current is null) {
                    throw new Exception("Failed to complete initial sketch step");
                } else {
                    final.Add(generator.TargetId, current);
                }
            }

            return new(
                type,
                final[gen_top.TargetId],
                final[gen_bot.TargetId],
                final[gen_join.TargetId],
                final[gen_meet.TargetId]
            );
        }




        static async Task<FunctionDefinition> ReadTarget(FlexPath path, Identifier targetId) {
            var result_fn = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(path.PathWin), new[] { targetId }).Single();
            return PipelineUtil.SloppyFunctionalize(result_fn);
        }


        public async Task<Output> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);

            System.Console.WriteLine($"--- [Lattice] starting ---");

            List<LatticeDefs> output = new();

            foreach (var (type, compare) in Targets) {
                var result = await DoOne(dir.Append($"{type.Id}/"), type, compare);
                output.Add(result);

                PipelineUtil.WriteSketchFile(dir.Append($"{type.Id}.lattice.sk"), result.GetEach());
            }

            System.Console.WriteLine($"--- [Lattice] done ---");


            return new(output);
        }
    }
}
