//using Microsoft.Extensions.Logging;
//using Semgus.Operational;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;

//namespace Semgus.CommandLineInterface {
//    public class InterpreterHelper {
//        private class WorkItem {
//            public string semantics;
//            public string programStr;
//            public IDSLSyntaxNode program;
//            public IReadOnlyDictionary<string, object> input;
//            public InterpreterResult result;
//            public TimeSpan runtime = TimeSpan.Zero;
//        }

//        private readonly string _asmVersion;


//        public ILogger Logger { get; set; }

//        public InterpreterHelper() {
//            _asmVersion = Assembly.GetAssembly(typeof(InterpreterHost)).GetName().Version.ToString();
//        }

//        public IReadOnlyList<InterpreterOutputRow> Run(Located<InterpreterTask> source) {
//            var workItems = ExtractWorkItems(source);
//            return RunTasks(workItems, source.Value);
//        }

//        private List<WorkItem> ExtractWorkItems(Located<InterpreterTask> source) {
//            var task = source.Value;
//            return task.Files.Select(file => {
//                var (smt,sem) = ParseUtil.ParseFile(source.GetFilePath(file),Logger);
//                var lib = OperationalConverter.ProcessProductions(smt.Theories, sem.Chcs);
//                return (semantics: source.GetIdentifier(file), parser: DslParser.FromGrammar(lib, DSLSyntaxNode.Factory.Instance));
//            }).SelectMany(tu =>
//                task.Programs.Select(programStr =>
//                    new { semantics = tu.semantics, programStr = programStr, program = tu.parser.Parse(programStr) }
//                ).SelectMany(wip =>
//                    task.Examples.Select(ex => new WorkItem {
//                        semantics = wip.semantics,
//                        programStr = wip.programStr,
//                        program = wip.program,
//                        input = ex,//UnboxJsonProps(ex, wip.program), // todo cleanup
//                    })
//                )
//            ).ToList();
//        }

//        private List<InterpreterOutputRow> RunTasks(List<WorkItem> workItems, InterpreterTask task) {
//            var runCount = task.RunCount;
//            var interpreter = new InterpreterHost(task.MaxDepth);
            

//            Stopwatch stopwatch = new();

//            const int LOG_STEPS = 20;

//            foreach(var item in workItems) item.result = interpreter.RunProgram(item.program, item.input);

//            var n = workItems.Count;

//            var tt = Math.Max(1, runCount / LOG_STEPS);

//            // Enumerate over items in the *inner* loop.
//            // This mixes the set more, reducing the effect of earlier items taking longer (as observed when the loops are switched).
//            for (int run = 0; run < runCount; run++) {
//                if(run % tt == 0) Logger?.LogInformation("start iter {0}/{1}", run, runCount);
//                for (int i = 0; i < n; i++) {
//                    WorkItem item = workItems[i];
//                    stopwatch.Restart();
//                    interpreter.RunProgram(item.program, item.input);
//                    stopwatch.Stop();
//                    item.runtime += stopwatch.Elapsed;
//                }
//            }
//            Logger?.LogInformation("done");

//            double mu = 1.0 / runCount;

//            var table = workItems.Select(item => new InterpreterOutputRow {
//                InterpreterLibVersion = _asmVersion,
//                Semantics = item.semantics,
//                Program = item.programStr,
//                Input = Stringify(item.input),
//                Output = item.result.HasError ? "[err]" : Stringify(item.program.LabelOutputs(item.result.Values)),
//                MeanRunTimeMS = mu * item.runtime.TotalMilliseconds,
//            }).ToList();

//            return table;
//        }

//        public void AppendAggregates(List<InterpreterOutputRow> table) {
//            var perProgramExample = table.GroupBy(row => new { row.Program, row.Input }).Select(g => new InterpreterOutputRow {
//                InterpreterLibVersion = _asmVersion,
//                Semantics = "[all]",
//                Program = g.Key.Program,
//                Input = g.Key.Input,
//                Output = TryGetSingleValue(g.Select(r => r.Output), out var output) ? output : "[mixed]",
//                MeanRunTimeMS = g.Sum(e => e.MeanRunTimeMS)
//            }).ToList();

//            var perSemanticsProgram = table.GroupBy(row => new { row.Semantics, row.Program }).Select(g => {
//                return new InterpreterOutputRow {
//                    InterpreterLibVersion = _asmVersion,
//                    Semantics = g.Key.Semantics,
//                    Program = g.Key.Program,
//                    Input = "[all]",
//                    Output = TryGetSingleValue(g.Select(r => r.Output), out var output) ? output : "[mixed]",
//                    MeanRunTimeMS = g.Sum(e => e.MeanRunTimeMS)
//                };
//            }).ToList();

//            var perSemantics = perSemanticsProgram.GroupBy(row => row.Semantics).Select(g => new InterpreterOutputRow {
//                InterpreterLibVersion = _asmVersion,
//                Semantics = g.Key,
//                Program = "[all]",
//                Input = "[all]",
//                Output = TryGetSingleValue(g.Select(r => r.Output), out var output) ? output : "[mixed]",
//                MeanRunTimeMS = g.Sum(e => e.MeanRunTimeMS)
//            }).ToList();

//            var perSession = new InterpreterOutputRow {
//                InterpreterLibVersion = _asmVersion,
//                Semantics = "[all]",
//                Program = "[all]",
//                Input = "[all]",
//                Output = TryGetSingleValue(perSemantics.Select(r => r.Output), out var output) ? output : "[mixed]",
//                MeanRunTimeMS = perSemantics.Sum(e => e.MeanRunTimeMS)
//            };

//            int n_items = table.Count;
//            if (perProgramExample.Count < n_items) table.AddRange(perProgramExample);
//            if (perSemanticsProgram.Count < n_items) table.AddRange(perSemanticsProgram);
//            if (perSemantics.Count < perSemanticsProgram.Count) table.AddRange(perSemantics);
//            if (1 < perSemantics.Count) table.Add(perSession);
//        }

//        private static bool TryGetSingleValue<T>(IEnumerable<T> input, out T output) {
//            bool any = false;
//            T value = default;

//            var eq = EqualityComparer<T>.Default;

//            foreach (var next in input) {
//                if (any) {
//                    if (!eq.Equals(next,value)) {
//                        output = default;
//                        return false;
//                    }
//                } else {
//                    any = true;
//                    value = next;
//                }
//            }

//            output = value;
//            return any;
//        }

//        private static string Stringify(IReadOnlyDictionary<string, object> input) => string.Join(" ", input.Select(kvp => $"{kvp.Key}={kvp.Value.ToString().Replace('\n',' ').Replace("\r","")}"));

//        //private static Dictionary<string, object> UnboxJsonProps(IReadOnlyDictionary<string, object> e, IDSLSyntaxNode program) => e.ToDictionary(
//        //    kvp => kvp.Key,
//        //    kvp => UnboxJsonValue((JsonElement)kvp.Value, program.TryGetArgInfo(kvp.Key, out var info) ? info.Type : throw new KeyNotFoundException($"The variable {kvp.Key} is not an argument to the synth-fun"))
//        //);

//        //private static object UnboxJsonValue(object value, Type type) => type switch {
//        //    Type _ when type == typeof(Int32) => e.GetInt32(),
//        //    Type _ when type == typeof(String) => e.GetString(),
//        //    Type _ when type == typeof(Boolean) => e.GetBoolean(),
//        //    Type _ when type == typeof(SmtIntArray) => new SmtIntArray(e.EnumerateArray().Select(el=>el.GetInt32()).ToList()),
//        //    Type _ when type == typeof(SmtBitVec32) => e.TryGetInt32(out var ival) ? new SmtBitVec32((uint)ival) : SmtBitVec32.Parse(e.GetString()),
//        //    _ => throw new NotSupportedException(),
//        //};
//    }
//}