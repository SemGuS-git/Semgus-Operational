using CommandLine;
using CommandLine.Text;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public class Program {
        public enum Mode {
            solve,
            test
        }

        public class Options {
            [Value(0, MetaName = "mode", Required = true, HelpText = "Operation to perform. Must be one of [solve, test].")]
            public Mode Mode { get; set; }

            [Value(1, MetaName = "input", Min = 1, Required = true, HelpText = "One or more semgus files to process.")]
            public IEnumerable<string> InputFiles { get; set; }

            [Option('c', "config", Required = false, Default = "", HelpText = "TOML file for configuration options. If not included, will use a default bottom-up solver configuration.")]
            public string ConfigFile { get; set; }

            [Option('l', "loglevel", Required = false, Default = LogEventLevel.Debug, HelpText = "Logging level.")]
            public LogEventLevel LogLevel { get; set; }

            [Option('i', "writeIncremental", Required = false, Default = false, HelpText = "Update the output files after each input is finished, rather than at the end.")]
            public bool WriteIncremental { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples {
                get {
                    yield return new Example("Basic usage", new Options { Mode = Mode.solve, ConfigFile = "config.toml", InputFiles = new[] { "file1.sl, file2.sl" }, LogLevel = LogEventLevel.Debug });
                }
            }
        }

        static void Main(string[] args) {
            var parser = new CommandLine.Parser();
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithParsed(Run).WithNotParsed(errs => DisplayHelp(parserResult));
        }

        private static void Run(Options obj) {
            var config = string.IsNullOrWhiteSpace(obj.ConfigFile) ? Configuration.Default : Configuration.FromFile(obj.ConfigFile);
            var files = obj.InputFiles.ToList();

            // check files exist first
            foreach (var file in files) {
                if (!File.Exists(file)) throw new FileNotFoundException("Missing input file", file);
            }

            IRunner runner = obj.Mode switch {
                Mode.solve => new SolveRunner(config, obj),
                Mode.test => new TestRunner(config, obj),
                _ => throw new InvalidOperationException(),
            };

            runner.RunAll(files);
            runner.Close();
        }

        private static void DisplayHelp(ParserResult<Options> parserResult) {
            var helpText = HelpText.AutoBuild(parserResult, h => {
                h.Heading = "SemGuS Solver Frontend";
                h.Copyright = "Copyright (c) 2022 University of Wisconsin-Madison";
                h.AddEnumValuesToHelpText = true;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e);

            Console.WriteLine(helpText);
        }
    }
}