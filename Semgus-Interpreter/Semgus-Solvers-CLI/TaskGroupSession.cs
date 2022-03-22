using CsvHelper;
using Microsoft.Extensions.Logging;
using Semgus.Operational;
using Semgus.Solvers;
using Semgus.Solvers.Enumerative;
using Semgus.Util.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Semgus.CommandLineInterface {
    public class TaskGroupSession {
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

        public ILogger Logger { get; set; }
        public bool WriteIncremental { get; set; }

        public void Run(TaskGroupBatch batch) {// IReadOnlyList<Located<TaskGroup>> taskGroups) {
            var taskGroups = batch.TaskGroups;

            //if (taskGroups.Any(t => t.Value.UnitTestTasks.Count > 0)) {
            //    // Run unit tests
            //    Logger?.LogInformation("Begin unit tests...");
            //    var testsOk = RunUnitTestTasks(batch);
            //    if (testsOk) {
            //        Logger?.LogInformation("Passed all unit tests");
            //    } else {
            //        Logger?.LogError("One or more unit tests in {batch} failed; exiting early", batch.BatchName);
            //        return;
            //    }
            //}

            //if (taskGroups.Any(t => t.Value.InterpreterTasks.Count > 0)) {
            //    /// Run interpreter tasks
            //    Logger?.LogInformation("Begin interpreter tasks...");
            //    RunInterpreterTasks(batch);
            //}

            if (taskGroups.Any(t => t.Value.SynthesisTasks.Count > 0)) {
                // Run synth tasks
                Logger?.LogInformation("Begin synthesis tasks...");
                RunSynthesisTasks(batch);
            }

            Logger?.LogInformation("All tasks done");
        }

        //private bool RunUnitTestTasks(TaskGroupBatch batch) {
        //    var tasks = Located.Transfer(batch.TaskGroups, batch.RootFolder, taskGroup => taskGroup.UnitTestTasks);
        //    var helper = new UnitTestHelper() { Logger = Logger };

        //    List<UnitTestOutputRow> unitTestResults = new();

        //    bool ok = true;

        //    foreach (var result in tasks.SelectMany(helper.RunTask)) {
        //        unitTestResults.Add(result);
        //        LogUnitTestResult(result);
        //        ok &= result.Result == UnitTestResultCode.Pass;
        //    }

        //    WriteCsv(unitTestResults, batch.OutputPrefix, "unit-tests");

        //    return ok;
        //}

        private void LogUnitTestResult(UnitTestOutputRow result) {
            var level = result.Result switch {
                UnitTestResultCode.Pass => LogLevel.Debug,
                UnitTestResultCode.Fail => LogLevel.Information,
                UnitTestResultCode.UnhandledException => LogLevel.Error,
                _ => throw new NotImplementedException(),
            };

            Logger?.Log(level, "{Result}: in {File} run {Program} with {Input}; expected {Expected}, got {Actual}", result.Result, result.File, result.Program, result.Input, result.Expected, result.Actual);
        }

        //private void RunInterpreterTasks(TaskGroupBatch batch) {
        //    var tasks = Located.Transfer(batch.TaskGroups, batch.RootFolder, taskGroup => taskGroup.InterpreterTasks);
        //    var helper = new InterpreterHelper() { Logger = Logger };

        //    var resultEnum = tasks.SelectMany(helper.Run);
        //    var resultList = new List<InterpreterOutputRow>();

        //    const string SUFFIX = "interpretation";

        //    foreach (var result in resultEnum) {
        //        resultList.Add(result);
        //        if (WriteIncremental) {
        //            WriteCsv(resultList, batch.OutputPrefix, SUFFIX);
        //        }
        //    }

        //    helper.AppendAggregates(resultList);
        //    WriteCsv(resultList, batch.OutputPrefix, SUFFIX);
        //}

        private void RunSynthesisTasks(TaskGroupBatch batch) {
            var tasks = Located.Transfer(batch.TaskGroups, batch.RootFolder, taskGroup => taskGroup.SynthesisTasks);
            var helper = new SynthesisHelper() { Logger = Logger };

            var resultEnum = helper.RunAll(tasks, batch.BatchName);
            var resultList = new List<ISynthesisResult>();
            var summary = new List<SynthesisSummaryRow>();

            const string SUFFIX = "synth-result";

            foreach (var result in resultEnum) {
                resultList.Add(result);
                summary.Add(SynthesisSummaryRow.Convert(result));
                if (WriteIncremental) {
                    WriteJson(resultList, batch.OutputPrefix, SUFFIX);
                    WriteCsv(summary, batch.OutputPrefix, SUFFIX);
                }
            }

            if (!WriteIncremental) {
                WriteJson(resultList, batch.OutputPrefix, SUFFIX);
                WriteCsv(summary, batch.OutputPrefix, SUFFIX);
            }
        }

        void WriteJson<T>(List<T> list, string prefix, string suffix) {
            var json = JsonSerializer.Serialize(list, JSON_SERIALIZER_OPTIONS);
            File.WriteAllText($"{prefix}.{suffix}.json", json);
        }

        void WriteCsv<T>(List<T> list, string prefix, string suffix) {
            using var writer = new StreamWriter($"{prefix}.{suffix}.csv");
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(list);
        }
    }
}