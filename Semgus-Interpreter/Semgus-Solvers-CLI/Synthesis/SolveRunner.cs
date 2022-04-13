using CsvHelper;
using Microsoft.Extensions.Logging;
using Semgus.Operational;
using Semgus.Solvers;
using Semgus.Solvers.Enumerative;
using Semgus.Util.Json;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Semgus.CommandLineInterface {
    public class SolveRunner : IRunner {
        private static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() {
            WriteIndented = true,
            Converters = {
                    new PolymorphicWriteOnlyConverter<ISynthesisResult>(),
                    new ToStringWriteOnlyConverter<IDSLSyntaxNode>(),
                    new DelegateWriteOnlyConverter<WeightedGrammar>((writer, value, options) => JsonSerializer.Serialize(writer,value.ToPrettyDict(),options)),
                    new ToStringWriteOnlyConverter<TimeSpan>(),
                    new ToStringWriteOnlyConverter<BottomUpLoop.StopReason>(),
                }
        };

        private readonly Configuration _config;
        private readonly Program.Options _options;
        private readonly ILogger _logger;
        private readonly string _batchLabel;

        private readonly IDisposable _disposable;

        private readonly List<ISynthesisResult> _results = new();

        public SolveRunner(Configuration config, Program.Options options) {
            this._config = config;
            this._options = options;
            this._batchLabel = config.GetBatchLabel(Program.Mode.solve, DateTime.Now);
            var innerLogger = _config.MakeLogCfg(options.LogLevel, _batchLabel).CreateLogger();
            this._logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));
            this._disposable = innerLogger;
        }

        public void Close() {
            FlushResults();
            _disposable.Dispose();
        }

        private void FlushResults() {
            WriteCsv(_results.Select(SynthesisSummaryRow.Convert).ToList(), $"{_batchLabel}.csv");
            WriteJson(_results, $"{_batchLabel}.json");
        }

        public void Run(string inputFile) {
            var items = ParseUtil.TypicalItems.Acquire(inputFile, _logger);

            for (int i = 0; i < _config.Solvers.Count; i++) {
                SynthesisConfig cfg = _config.Solvers[i];
                var solver = EnumerativeSolverFactory.Instance.Instantiate(cfg);
                solver.Logger = _logger;

                var startTime = DateTime.Now;
                var result = solver.Run(items.Grammar, items.Constraint);
                result.InputInfo = new(startTime, inputFile, $"v{i}", _batchLabel);

                _results.Add(result);
                if(_options.WriteIncremental) {
                    FlushResults();
                }
            }
        }

        static void WriteJson<T>(List<T> list, string filePath) {
            var json = JsonSerializer.Serialize(list, JSON_SERIALIZER_OPTIONS);
            File.WriteAllText(filePath, json);
        }

        static void WriteCsv<T>(List<T> list, string filePath) {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(list);
        }
    }
}