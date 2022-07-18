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
                //"sum-by-while.sl",

                //"impv-demo.sl",
               //"max2-exp.sl",
               //"max3-exp.sl",
               // "regex4-simple.sl",
               // "regex4-either-pair.sl",
               // "polynomial.sl",
               // "regex6-padded-cycle.sl",
               // "regex8-aa.sl"
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



            var start_symbol = items.Constraint.StartSymbol;
            Debug.Assert(items.Grammar.Nonterminals.Contains(start_symbol));

            var g_idx = GrammarIndexing.From(start_symbol, items.Grammar);

            var abs_sem_raw = TupleLibrary.From(g_idx, items.Grammar.Productions, items.Library); // May throw


            bool reuse_prev = false;
            if (!reuse_prev) CleanPreviousOutput(dir);
            PrepOutputDirectory(dir);


            {
                var json_file = dir.Append("concrete_sem.json");
                File.WriteAllText(json_file.PathWin, OutputFormat.ConcConverters.Serialize(items.Library));
                Console.WriteLine("--- Wrote conc file ---");
            }
            {
                var json_file = dir.Append("specification.json");
                File.WriteAllText(json_file.PathWin, OutputFormat.SpecConverters.Serialize(g_idx,items.Grammar.Productions,items.Constraint));
                Console.WriteLine("--- Wrote spec file ---");
            }

            var (cores,lattices) = await RunPipeline(dir, items.Library, abs_sem_raw, reuse_prev);

            // apply monotonicities to abstract sem
            TupleLibrary abs_sem = abs_sem_raw.WithMonotonicitiesFrom(cores.QueryFunctions);


            Console.WriteLine("--- Abs sem monotonized ---");

            {
                var json_file = dir.Append("abstract_sem.json");
                File.WriteAllText(json_file.PathWin, OutputFormat.Converters.Serialize(abs_sem, lattices.Lattices));
                Console.WriteLine("--- Wrote abs file ---");
            }

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

        static void CleanPreviousOutput(FlexPath dir) {
            if (Directory.Exists(dir.PathWin)) {
                Directory.Delete(dir.PathWin, true);
            }
        }

        static void PrepOutputDirectory(FlexPath dir) {
            if (!Directory.Exists(dir.PathWin)) {
                Directory.CreateDirectory(dir.PathWin);
            }
        }

        static async Task<(MonotonicityStep.Output cores, LatticeStep.Output lattices)> RunPipeline(FlexPath dir, InterpretationLibrary conc, TupleLibrary abs_sem_raw, bool reuse_previous = false) {

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
                    theta = await step.Execute(dir.Append("step_4_lattice/"),false);


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