//using Microsoft.Extensions.Logging;
//using Semgus.Constraints;
//using Semgus.Operational;
//using Semgus.Solvers.Enumerative;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;


//namespace Semgus.CommandLineInterface {
//    using R = UnitTestResultCode;

//    public class UnitTestHelper {
//        private class WorkItem {
//            public string semantics;
//            public string programStr;
//            public IDSLSyntaxNode program;
//            public IReadOnlyDictionary<string, object> input;
//            public InterpreterResult output;
//            public TimeSpan runtime = TimeSpan.Zero;
//        }


//        public ILogger Logger { get; set; }

//        static string Stringify(IReadOnlyDictionary<string, object> obj) => JsonSerializer.Serialize(obj);
//        static string Stringify(Exception err) => $"<error: {err}>";
//        public const string ERROR_VALUE = "<error>";


//        private static UnitTestOutputRow AssertConstraintResponse(UnitTestOutputRow.Factory rowFactory, InterpreterHost host, IDSLSyntaxNode program, InductiveConstraint constraint, bool shouldBeSolution) {
//            if (constraint is null) return rowFactory.Catch(new NullReferenceException("Missing constraints for synth-term solution check"));
//            try {
//                var isSolution = new InductiveBasicReceiver(host, constraint).IsSolutionDebug(program,out var report);
//                return rowFactory.FromConstraintCheck(shouldBeSolution, isSolution, report);
//            } catch (Exception e) {
//                return rowFactory.Catch(e);
//            }
//        }

//        private static UnitTestOutputRow AssertInputOutput(UnitTestOutputRow.Factory rowFactory, InterpreterHost host, IDSLSyntaxNode program, IReadOnlyDictionary<string, object> example) {
//            try {
                
//                var (input, expected) = program.SplitInputOutput(example);
//                if (expected.Count == 0) throw new ArgumentException($"Unit test must valuate at least one output variable");

//                var result = host.RunProgram(program, input);

//                if (result.HasError) {
//                    return rowFactory.Fail(input, Stringify(expected), result.Error.PrettyPrint(false));
//                } else {
//                    var actual = program.LabelOutputs(result.Values);
//                    return rowFactory.FromStatus(ExpectedInActual(expected, actual), input, Stringify(expected), Stringify(actual));
//                }

//            } catch (Exception e) {
//                return rowFactory.Catch(Stringify(example), e);
//            }
//        }

//        private static UnitTestOutputRow AssertError(UnitTestOutputRow.Factory rowFactory, InterpreterHost host, IDSLSyntaxNode program, IReadOnlyDictionary<string, object> example) {
//            try {
//                var (input, output) = program.SplitInputOutput(example);
//                if (output.Count > 0) throw new ArgumentException($"Unit test for an error should not contain output variables (found {{{string.Join(", ", output.Keys)}}} )");

//                var result = host.RunProgram(program, example);
//                if (result.HasError) {
//                    return rowFactory.Pass(input, ERROR_VALUE, result.Error.PrettyPrint(false));
//                } else {
//                    var actual = program.LabelOutputs(result.Values);
//                    return rowFactory.Fail(input, ERROR_VALUE, Stringify(actual));
//                }
//            } catch (Exception e) {
//                return rowFactory.Catch(Stringify(example), e);
//            }
//        }


//        public IEnumerable<UnitTestOutputRow> RunTask(Located<UnitTestTask> source) {
//            // todo add logging

//            var task = source.Value;

//            foreach (var file in task.Files) {

//                // This block may throw unhandled parse-time errors
//                var items = ParseUtil.TypicalItems.Acquire(source.GetFilePath(file));

//                var parser = DslParser.FromGrammar(items.Library, DSLSyntaxNode.Factory.Instance);
                

//                foreach (var test in task.Tests) {
//                    var rowFactory = new UnitTestOutputRow.Factory(source.GetIdentifier(file), test.Program);

//                    IDSLSyntaxNode program = null;
//                    Exception parseError = null;
//                    try {
//                        program = parser.Parse(test.Program);
//                    } catch (Exception e) {
//                        parseError = e;
//                    }
//                    if (parseError is not null) {
//                        yield return rowFactory.Catch(parseError);
//                        continue;
//                    }

//                    var host = new InterpreterHost(test.MaxDepth);

//                    if (test.SynthFunSolution.HasValue) {
//                        yield return AssertConstraintResponse(rowFactory, host, program, items.Constraint, test.SynthFunSolution.Value);
//                    }

//                    // Expect values
//                    foreach (var example in test.Examples) {
//                        yield return AssertInputOutput(rowFactory, host, program, example);
//                    }

//                    // Expect error
//                    foreach (var example in test.ErrorExamples) {
//                        yield return AssertError(rowFactory, host, program, example);
//                    }
//                }
//            }
//        }

//        private static R ExpectedInActual(IReadOnlyDictionary<string, object> expected, IReadOnlyDictionary<string, object> actual) {
//            foreach (var kvp in expected) {
//                if (!actual.TryGetValue(kvp.Key, out var actualVal) || !kvp.Value.Equals(actualVal)) return R.Fail;
//            }
//            return R.Pass;
//        }
//    }
//}