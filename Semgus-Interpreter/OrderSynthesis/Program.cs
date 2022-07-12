#define NOT_REUSE

using Microsoft.Extensions.Logging;
using Semgus.CommandLineInterface;
using Semgus.Operational;
using Semgus.OrderSynthesis.AbstractInterpretation;
using Semgus.OrderSynthesis.IntervalSemantics;
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
            var head = args[0];

            var nex = new List<string>();

            foreach (var target in new[] {
              //  "impv-demo.sl",
               "max2-exp.sl",
               //"max3-exp.sl",
                //"regex4-simple.sl",
                //"regex4-either-pair.sl",
               // "polynomial.sl",
                //"regex6-padded-cycle.sl",
                //"regex8-aa.sl"
            }) {
                var file = head + target;
                Debug.Assert(File.Exists(file), "Missing input file {0}", file);
                nex.Add(file);
            }
            
            foreach(var file in nex) {
                try {
                    await Main(file);
                } catch(Exception e) {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"Hit exception during work on {file}");
                    Console.WriteLine(e.ToString());
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        static async Task Main(string file) {

            Debug.Assert(File.Exists(file), "Missing input file {0}", file);

            string fname = Path.GetFileName(file);
            FlexPath dir = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/{fname}/");

            var items = ParseUtil.TypicalItems.Acquire(file); // May throw
            var abs_sem_raw = InitialStuff.From(items.Grammar, items.Library); // May throw

            var (cores,lattices) = await RunPipeline(dir, abs_sem_raw, false);

            // apply monotonicities to abstract sem
            InitialStuff abs_sem = abs_sem_raw.WithMonotonicitiesFrom(cores.QueryFunctions);


            Console.WriteLine("--- Abs sem monotonized ---");

            var json_file = dir.Append("result.json");

            File.WriteAllText(json_file.PathWin, OutputFormat.Converters.Serialize(abs_sem, lattices.Lattices));


            //var cfg = new ConfigParameters {
            //    CostFunction = TermCostFunction.Size,
            //    Reductions = new() { ReductionMethod.ObservationalEquivalence }
            //};

            //{
            //    var logCfg = new LoggerConfiguration()
            //        .Enrich.FromLogContext()
            //        .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            //        .WriteTo.Console()
            //        .WriteTo.File($"demo.{fname}.log");

            //    using var innerLogger = logCfg.CreateLogger();
            //    var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));

            //    var solver = new TopDownSolver(cfg) { Logger = logger };
            //    solver.TempRedTwo = new AbstractReduction(items.Constraint.Examples,abs_sem);

            //    var sw = new Stopwatch();
            //    sw.Start();
            //    var synth_res = solver.Run(items.Grammar, items.Constraint);
            //    sw.Stop();
            //    logger.LogInformation("Top-down solver with abstract reduction took {0}", sw.Elapsed);

            //}
            //{
            //    var logCfg = new LoggerConfiguration()
            //        .Enrich.FromLogContext()
            //        .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            //        .WriteTo.Console()
            //        .WriteTo.File("demo.bottomup.log");

            //    using var innerLogger = logCfg.CreateLogger();
            //    var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));


            //    var solver = new BottomUpSolver(cfg) { Logger = logger };
            //    var sw = new Stopwatch();
            //    sw.Start();
            //    var synth_res = solver.Run(items.Grammar, items.Constraint);
            //    sw.Stop();
            //    logger.LogInformation("Bottom-up solver with OE reduction took {0}", sw.Elapsed);
            //}

            Console.WriteLine("--- Did synth ---");
        }

        static async Task<(MonotonicityStep.Output cores, LatticeStep.Output lattices)> RunPipeline(FlexPath dir, InitialStuff abs_sem_raw, bool reuse_previous = false) {
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
                MonotonicityStep.Output rho;
                {
                    var step = new MonotonicityStep(abs_sem_raw);

                    //state = new(PipelineState.Step.Initial, step.StructTypeMap, step.Structs);

                    rho = await step.Execute(dir.Append("step_1_mono/"), reuse_previous); // May throw

                    //state = state with {
                    //    Reached = PipelineState.Step.Monotonicity,
                    //    Comparisons = result.Comparisons,
                    //    Monotonicities = result.QueryFunctionMono
                    //    LabeledTransformers = result.LabeledTransformers
                    //};
                }

                {
                    var result = await OrderExpansionStep.ExecuteLoop(dir.Append("step_2_expand/"), rho, reuse_previous); // May throw
                    rho = rho with {  Comparisons = result.Comparisons };
                    //state = state with { Reached = PipelineState.Step.OrderExpansion, Comparisons = result.Comparisons };
                }

                try {

                    var step = new SimplificationStep(rho);
                    var result = await step.Execute(dir.Append("step_3_simplify/"), reuse_previous);
                    rho = rho with { Comparisons = result.Comparisons };

                    //state = state with { Reached = PipelineState.Step.Simplification, Comparisons = result.Comparisons };

                } catch (Exception e) {
                    // Don't treat this as a hard stop
                    Console.Error.Write(e);
                    Console.Error.Write("Continuing");
                }

                LatticeStep.Output theta;

                {

                    var step = new LatticeStep(rho.ZipComparisonsToTypes());
                    theta = await step.Execute(dir.Append("step_4_lattice/"),reuse_previous);


                    //state = state with { Reached = PipelineState.Step.Lattice, Lattices = result.Lattices };
                }

                //if (!reuse_previous) {
                //    // TODO: load compares and monotonicities into abstract interpretation framework
                //    await PipelineUtil.WriteState(dir.Append("result/"), state);
                //}

                Console.WriteLine("--- Pipeline finished ---");
                return (rho,theta);
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