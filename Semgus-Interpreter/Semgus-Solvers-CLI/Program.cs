using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public  class Program {
        public class Options {
            [Value(0, MetaName = "input", Min = 1, Required = true, HelpText = "One or more input files to be processed. May be either TOML batch configurations or individual semgus files.")]
            public IEnumerable<string> InputFiles { get; set; }

            [Option('l', "loglevel", Required = false, Default = LogEventLevel.Debug, HelpText = "Logging level.")]
            public LogEventLevel LogLevel { get; set; }

            [Option("runParseCheck", Required =false,Default =false, HelpText ="Instead of running tasks, check that each involved Semgus file can be processed and output any errors.")]
            public bool RunParseCheck { get; set; }

            [Option("writeIncremental", Required = false, Default = false, HelpText = "Update output files as each task is completed.")]
            public bool WriteIncremental { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples {
                get {
                    yield return new Example("One file, incremental output", new Options { InputFiles = new[] { "examples/batch.toml" }, WriteIncremental = true });
                    yield return new Example("Two files with minimal logging", new Options { InputFiles = new[] { "batch0.toml", "batch1.toml" }, LogLevel = LogEventLevel.Information });
                    yield return new Example("Run parse check with maximum logging", new Options { RunParseCheck = true, LogLevel = LogEventLevel.Verbose, InputFiles = new[] { "examples/batch.toml" } });
                    yield return new Example("Semgus files using default solver settings", new Options { InputFiles = new[] { "problem0.sem", "problem1.sem" }});
                }
            }
        }

        static void Main(string[] args) {
            var parser = new CommandLine.Parser();
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithParsed(Run).WithNotParsed(errs => DisplayHelp(parserResult));
        }

        private static void Run(Options obj) {

            var batches = ReadInputFiles(obj.InputFiles.ToList());

            if (obj.RunParseCheck) {
                foreach (var batch in batches) {
                    RunParseCheck(batch, obj.LogLevel);
                }
            } else {
                foreach (var batch in batches) {
                    RunTaskGraph(batch, obj.LogLevel, obj.WriteIncremental);
                }
            }
        }

        private static IReadOnlyList<TaskGroupBatch> ReadInputFiles(IReadOnlyList<string> inputFiles) {
            bool isToml = false, isSem = false;

            foreach(var file in inputFiles) {
                if (file.EndsWith(".sem")) {
                    if (isToml) throw new ArgumentException("May not run on a mixture of .sem and .toml files");
                    isSem = true;
                } else if (file.EndsWith(".toml")) {
                    if (isSem) throw new ArgumentException("May not run on a mixture of .sem and .toml files");
                    isToml = true;
                } else {
                    throw new ArgumentException("All input files must have extension .sem or .toml");
                }
                if (!File.Exists(file)) throw new FileNotFoundException("Missing input file", file);
            }

            if(isSem) {
                return new[] { TaskGroupBatch.DefaultSynthTask(Directory.GetCurrentDirectory(), inputFiles) };
            }
            if(isToml) {
                return inputFiles.Select(TaskGroupBatch.ReadFileTree).ToList();
            }
            return Array.Empty<TaskGroupBatch>();
        }

        private static void RunParseCheck(TaskGroupBatch batch, LogEventLevel logLevel) {
            var logFilePath = batch.OutputPrefix + ".parse-check.log";
            using var innerLogger = MakeLogCfg(logLevel, logFilePath).CreateLogger();
            var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(Program));

            var session = new ParseCheckSession() { Logger = logger };
            session.Run(batch);
        }


        static void RunTaskGraph(TaskGroupBatch batch, LogEventLevel logLevel, bool writeIncremental) {
            var logFilePath = batch.OutputPrefix + ".log";
            using var innerLogger = MakeLogCfg(logLevel, logFilePath).CreateLogger();
            var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(Program));

            var session = new TaskGroupSession { Logger = logger, WriteIncremental = writeIncremental };
            session.Run(batch);
        }


        private static LoggerConfiguration MakeLogCfg(LogEventLevel logLevel, string logFilePath) =>
            new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console()
                .WriteTo.File(logFilePath);

        private static void DisplayHelp(ParserResult<Options> parserResult) {
            var helpText = HelpText.AutoBuild(parserResult, h => {
                h.Heading = "SemGuS Solver Frontend";
                h.Copyright = "Copyright (c) 2021 University of Wisconsin-Madison";
                h.AddEnumValuesToHelpText = true;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e);

            Console.WriteLine(helpText);
        }
    }
}