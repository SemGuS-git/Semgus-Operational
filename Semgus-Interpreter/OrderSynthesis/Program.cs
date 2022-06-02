#define NOT_REUSE

using Microsoft.Extensions.Logging;
using Semgus.CommandLineInterface;
using Semgus.Operational;
using Semgus.OrderSynthesis.AbstractInterpretation;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.Subproblems;
using Semgus.Solvers.Enumerative;
using Serilog;
using Serilog.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Semgus.OrderSynthesis {

    public class Program {

        static async Task Main(string[] args) {
            var file = args[0];

            Debug.Assert(File.Exists(file), "Missing input file {0}", file);

            FlexPath dir = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/{Path.GetFileName(file)}/");

            var items = ParseUtil.TypicalItems.Acquire(file); // May throw
            var preinit = AbstractPreInit.From(items.Grammar, items.Library); // May throw

            var result = await RunPipeline(dir, preinit, false);
            var abs_sem = preinit.Hydrate(result.Lattices!, result.LabeledTransformers!);


            Console.WriteLine("--- Abs sem constructed ---");


            var cfg = new ConfigParameters {
                CostFunction = TermCostFunction.Size,
                Reductions = new() { ReductionMethod.ObservationalEquivalence }
            };

            {
                var logCfg = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                    .WriteTo.Console()
                    .WriteTo.File("demo.topdown.log");

                using var innerLogger = logCfg.CreateLogger();
                var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));

                var solver = new TopDownSolver(cfg) { Logger = logger };
                solver.TempRedTwo = new AbstractReduction(items.Constraint.Examples,abs_sem);

                var sw = new Stopwatch();
                sw.Start();
                var synth_res = solver.Run(items.Grammar, items.Constraint);
                sw.Stop();
                logger.LogInformation("Top-down solver with abstract reduction took {0}", sw.Elapsed);

            }
            {
                var logCfg = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                    .WriteTo.Console()
                    .WriteTo.File("demo.bottomup.log");

                using var innerLogger = logCfg.CreateLogger();
                var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));


                var solver = new BottomUpSolver(cfg) { Logger = logger };
                var sw = new Stopwatch();
                sw.Start();
                var synth_res = solver.Run(items.Grammar, items.Constraint);
                sw.Stop();
                logger.LogInformation("Bottom-up solver with OE reduction took {0}", sw.Elapsed);
            }

            Console.WriteLine("--- Did synth ---");
        }

        static async Task<PipelineState> RunPipeline(FlexPath dir, AbstractPreInit preInit, bool reuse_previous = false) {
            if (!reuse_previous) {
                if (Directory.Exists(dir.PathWin)) {
                    Directory.Delete(dir.PathWin, true);
                }
                Directory.CreateDirectory(dir.PathWin);
            } else {
                //if (!Directory.Exists(dir.PathWin)) throw new DirectoryNotFoundException(dir.PathWin);
                //var temp_storage_dir = dir.Append("../_temp/");
                //if (Directory.Exists(temp_storage_dir.PathWin)) throw new InvalidOperationException($"There's already a directory in our temp location {temp_storage_dir.PathWin}");
                //Directory.CreateDirectory(temp_storage_dir.PathWin);

                //var target_dir = dir.Append("step_1_mono/");
                //var subtargets = new[] { "input.sk", "result.sk", "result.holes.xml" };
                //foreach (var s in subtargets) {
                //    File.Copy(target_dir.Append(s).PathWin, temp_storage_dir.Append(s).PathWin);
                //}
                //Directory.Delete(dir.PathWin, true);
                //Directory.CreateDirectory(target_dir.PathWin);
                //foreach (var s in subtargets) {
                //    File.Copy(temp_storage_dir.Append(s).PathWin, target_dir.Append(s).PathWin);
                //}
                //Directory.Delete(temp_storage_dir.PathWin, true);
            }
            PipelineState? state = null;

            try {
                {
                    var step = new MonotonicityStepBuilder(preInit).Build();

                    state = new(PipelineState.Step.Initial, step.StructTypeMap, step.Structs);

                    var result = await step.Execute(dir.Append("step_1_mono/"), reuse_previous); // May throw

                    state = state with {
                        Reached = PipelineState.Step.Monotonicity,
                        Comparisons = result.Comparisons,
                        LabeledTransformers = result.LabeledTransformers
                    };
                }

                {
                    var result = await OrderExpansionStep.ExecuteLoop(dir.Append("step_2_expand/"), state, reuse_previous); // May throw
                    state = state with { Reached = PipelineState.Step.OrderExpansion, Comparisons = result.Comparisons };
                }

                try {

                    var step = new SimplificationStep(new(state.StructTypeMap, state.StructTypeList, state.Comparisons));
                    var result = await step.Execute(dir.Append("step_3_simplify/"), reuse_previous);
                    state = state with { Reached = PipelineState.Step.Simplification, Comparisons = result.Comparisons };

                } catch (Exception e) {
                    // Don't treat this as a hard stop
                    Console.Error.Write(e);
                    Console.Error.Write("Continuing");
                }

                {
                    var step = new LatticeStep(state.StructTypeList.Zip(state.Comparisons!));
                    var result = await step.Execute(dir.Append("step_4_lattice/"),reuse_previous);
                    state = state with { Reached = PipelineState.Step.Lattice, Lattices = result.Lattices };
                }

                if (!reuse_previous) {
                    // TODO: load compares and monotonicities into abstract interpretation framework
                    await PipelineUtil.WriteState(dir.Append("result/"), state);
                }

                Console.WriteLine("--- Pipeline finished ---");
                return state;
            } catch (Exception e) {
                Console.Error.Write(e);
                Console.Error.Write("Halting");
                if (state is not null) {
                    var stash = dir.Append("incomplete_result/");
                    await PipelineUtil.WriteState(stash, state);
                }
                throw;
            }

        }

    }
}