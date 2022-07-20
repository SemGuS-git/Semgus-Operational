#define NOT_REUSE

using CommandLine;
using CommandLine.Text;
using Semgus.Operational;
using Semgus.OrderSynthesis.IntervalSemantics;
using Semgus.OrderSynthesis.Subproblems;
using System.Diagnostics;

namespace Semgus.OrderSynthesis {

    public class Program {


        public class Options {

            [Value(0, MetaName = "input", Required = true, HelpText = "Relative path to the input Semgus file.")]
            public string InputFile { get; set; }

            [Option('o', "output", Required = false, Default = "", HelpText = "Relative path to the output folder. Folder will be created if necessary. If not set, the output folder will appear alongside the input file.")]
            public string OutputPath { get; set; }

            [Option('f', "overwrite", Required = false, Default = false, HelpText = "If the output path exists, overwrite it.")]
            public bool Overwrite { get; set; }

            [Option("run-mono", Required = false, Default = false, HelpText = "Run the monotonicity pipeline to produce an interval abstract semantics. (Requires a WSL environment with Sketch installed)")]
            public bool RunMonotonicityPipeline { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples {
                get {
                    yield return new Example("Basic", new Options { InputFile="problem.sl", });
                }
            }
        }

        static async Task Main(string[] args) {

            var parser = new CommandLine.Parser();

            var parserResult = parser.ParseArguments<Options>(args);

            await parserResult.WithNotParsed(errs => DisplayHelp(parserResult)).WithParsedAsync(Main);

        }
        private static void DisplayHelp(ParserResult<Options> parserResult) {
            var helpText = HelpText.AutoBuild(parserResult, h => {
                h.Heading = "SemGuS Operational Semantics Tool";
                h.Copyright = "Copyright (c) 2022 University of Wisconsin-Madison";
                h.AddEnumValuesToHelpText = true;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e);

            Console.WriteLine(helpText);
        }

        static async Task Main(Options opt) { 
            FlexPath input_file = FlexPath.FromWin(Path.GetFullPath(opt.InputFile));

            if(!File.Exists(input_file.PathWin)) {
                throw new FileNotFoundException("Missing input file", input_file.PathWin);
            }


            
            var items = ParseUtil.TypicalItems.Acquire(input_file.PathWin); // May throw

            var start_symbol = items.Constraint.StartSymbol;
            Debug.Assert(items.Grammar.Nonterminals.Contains(start_symbol));

            var g_idx = GrammarIndexing.From(items.Grammar);


            var dir = FlexPath.FromWin(string.IsNullOrWhiteSpace(opt.OutputPath) ? input_file.PathWin + ".out/" : Path.GetFullPath(opt.OutputPath));

            var dir_info = new DirectoryInfo(dir.PathWin);

            if(File.Exists(dir.PathWin)) {
                if(opt.Overwrite) {
                    File.Delete(dir.PathWin);
                } else {
                    throw new IOException($"Output path {dir.PathWin} is occupied by a file");
                }
            } else if (dir_info.Exists) {
                if(opt.Overwrite) {
                    dir_info.Delete(true);
                } else {
                    throw new IOException($"Output path {dir.PathWin} is occupied by a directory");
                }
            }

            dir_info.Create();

            Console.WriteLine($"Writing output to directory {dir_info.FullName}");

            {
                var json_file = dir.Append("concrete_sem.json");
                File.WriteAllText(json_file.PathWin, OutputFormat.ConcConverters.Serialize(items.Library));
                Console.WriteLine("--- Wrote conc file ---");
            }
            {
                var json_file = dir.Append("specification.json");
                File.WriteAllText(json_file.PathWin, OutputFormat.SpecConverters.Serialize(g_idx,items.Grammar,items.Constraint));
                Console.WriteLine("--- Wrote spec file ---");
            }

            if (opt.RunMonotonicityPipeline) {

                var abs_sem_raw = TupleLibrary.From(g_idx, items.Grammar.Productions, items.Library); // May throw

                var (cores, lattices) = await RunPipeline(dir, items.Library, abs_sem_raw);

                // apply monotonicities to abstract sem
                TupleLibrary abs_sem = abs_sem_raw.WithMonotonicitiesFrom(cores.QueryFunctions);


                Console.WriteLine("--- Abs sem monotonized ---");

                {
                    var json_file = dir.Append("abstract_sem.json");
                    File.WriteAllText(json_file.PathWin, OutputFormat.Converters.Serialize(abs_sem, lattices.Lattices));
                    Console.WriteLine("--- Wrote abs file ---");
                }
            }

            Console.WriteLine("--- Done ---");
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