using CsvHelper;
using Microsoft.Extensions.Logging;
using Semgus.Operational;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Semgus.CommandLineInterface {
    public class TestRunner : IRunner {
        private readonly Configuration _config;
        private readonly Program.Options _options;
        private readonly ILogger _logger;
        private readonly string _batchLabel;

        private readonly IDisposable _disposable;

        private readonly List<TestOutputRow> _rows = new();

        public TestRunner(Configuration config, Program.Options options) {
            this._config = config;
            this._options = options;
            this._batchLabel = config.GetBatchLabel(Program.Mode.test, DateTime.Now);
            var innerLogger = _config.MakeLogCfg(options.LogLevel, _batchLabel).CreateLogger();
            this._logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(SolveRunner));
            this._disposable = innerLogger;
        }

        public void Run(string inputFile) {
            foreach(var row in GetRows(inputFile)) {
                LogRow(row);
                _rows.Add(row);
            }
            if (_options.WriteIncremental) {
                FlushResults();
            }
        }

        private void LogRow(TestOutputRow result) {
            var level = result.Result switch {
                TestResultCode.Pass => LogLevel.Debug,
                TestResultCode.Fail => LogLevel.Information,
                TestResultCode.UnhandledException => LogLevel.Warning,
                _ => throw new NotImplementedException(),
            };

            if (_logger.IsEnabled(level)) {
                _logger.Log(level, result.ToString());
            }
        }

        private void FlushResults() {
            WriteCsv(_rows, $"{_batchLabel}.csv");
        }

        public void Close() {
            FlushResults();
            _disposable.Dispose();
        }

        static void WriteCsv<T>(List<T> list, string filePath) {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(list);
        }

        private IEnumerable<TestOutputRow> GetRows(string inputFile) {

            if(!TryAcquire(inputFile,_logger,out var items,out var err)) {
                _logger.LogWarning("Failed to validate file {inputFile}: {err}", inputFile, err);
                yield return new TestOutputRow(inputFile, "", TestResultCode.UnhandledException, "<file-ok>", "", err.Message);
                yield break;
            }

            var host = new InterpreterHost(_config.StackLimit);

            int n_ok = 0;
            int n_row = 0;
            foreach(var test in items.Tests) {
                var program = test.Program;
                var rowFactory = new TestOutputRow.Factory(inputFile, CallPrettyPrint(program));

                foreach (var batch in test.ArgLists) {
                    var row = RunOne(host, program, rowFactory, batch);
                    n_row++;
                    if (row.Result == TestResultCode.Pass) n_ok++;
                    yield return row;
                }
            }

            foreach (var solnBlock in items.Solutions) {
                if(solnBlock.SynthFun != items.SynthFun) {
                    throw new NotSupportedException("Multiple synth-funs not yet supported");
                }

                foreach (var program in solnBlock.Solutions) {
                    bool soln_ok = true;
                    var rowFactory = new TestOutputRow.Factory(inputFile, CallPrettyPrint(program));

                    foreach (var constraint in items.Constraint.Examples) {
                        var row = RunOne(host, program, rowFactory, constraint.Values);
                        n_row++;
                        var ok = row.Result == TestResultCode.Pass;
                        if (ok) n_ok++;
                        soln_ok &= ok;
                        yield return row;
                    }

                    var soln_row = rowFactory.FromStatus(soln_ok ? TestResultCode.Pass : TestResultCode.Fail, "<synth-fun-soln>", true.ToString(), soln_ok.ToString());
                    n_row++;
                    if (soln_ok) n_ok++;
                    yield return soln_row;
                }
            }

            if(n_ok == n_row) {
                _logger.LogInformation("[All {n_row}] tests passed for file {inputFile}", n_row, inputFile);
            } else {
                _logger.LogWarning("[{n_ok} / {n_row}] tests passed for file {inputFile}", n_ok, n_row, inputFile);
            }
        }

        private static bool TryAcquire(string inputFile, ILogger logger, out ParseUtil.TypicalItems items, out Exception err) {
            try {
                items = ParseUtil.TypicalItems.Acquire(inputFile, logger);
                err = default;
                return true;
            } catch(Exception e) {
                items = default;
                err = e;
                return false;
            }
        }

        private static TestOutputRow RunOne(InterpreterHost host, IDSLSyntaxNode program, TestOutputRow.Factory rowFactory, object[] batch) {
            try {
                var result = host.RunProgram(program, batch);

                if (result.HasError) {
                    return rowFactory.Fail(GetInputStr(program, batch), GetOutputStr(program, batch), result.Error.PrettyPrint(true));
                } else {
                    var status = Correctness(program, batch, result.Values);
                    return rowFactory.FromStatus(status, GetInputStr(program, batch), GetOutputStr(program, batch), GetOutputStr(program, result.Values));
                }
            } catch (Exception e) {
                return rowFactory.Catch(GetInputStr(program, batch), e);
            }
        }

        private static TestResultCode Correctness(IDSLSyntaxNode program, object?[] expected, object?[] actual) {
            foreach (var outputVar in program.ProductionRule.OutputVariables) {
                var i = outputVar.Index;
                if(expected[i] is not null && !expected[i].Equals(actual[i])) return TestResultCode.Fail;
            }
            return TestResultCode.Pass;
            throw new NotImplementedException();
        }

        private static string PrintSelectedValues(IEnumerable<VariableInfo> targets, object?[] batch) {
            var sb = new StringBuilder();
            sb.Append('{');
            bool x = false;
            foreach(var v in targets) {
                if(x) {
                    sb.Append(' ');
                } else {
                    x = true;
                }
                sb.Append(v.Name);
                sb.Append(':');
                sb.Append(batch[v.Index]?.ToString() ?? "any");
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string GetInputStr(IDSLSyntaxNode program, object?[] batch) => PrintSelectedValues(program.ProductionRule.InputVariables, batch);
        private static string GetOutputStr(IDSLSyntaxNode program, object?[] batch) => PrintSelectedValues(program.ProductionRule.OutputVariables, batch);

        private static string CallPrettyPrint(IDSLSyntaxNode node) {
            var sb = new StringBuilder();
            node.PrettyPrint(sb);
            return sb.ToString();
        }
    }
}