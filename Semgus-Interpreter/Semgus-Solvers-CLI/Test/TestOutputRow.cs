using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;


namespace Semgus.CommandLineInterface {
    using R = TestResultCode;

    public record TestOutputRow(string File, string Program, R Result, string Input, string Expected, string Actual) {

        public record Factory(string File, string Program) {
            public TestOutputRow FromStatus(R result, string input, string expected, string actual) => new(File, Program, result, input, expected, actual);

            public TestOutputRow FromConstraintCheck(bool expected, bool actual, string report) => new(File, Program, expected == actual ? R.Pass : R.Fail, "<synth-term-constraints>", expected ? "<sat>" : "<unsat>", (actual ? "<sat>:\n" : "<unsat>:\n") + report );

            public TestOutputRow Pass(string input, string expected, string actual) => FromStatus(R.Pass, input, expected, actual);
            public TestOutputRow Fail(string input, string expected, string actual) => FromStatus(R.Fail, input, expected, actual);

            public TestOutputRow Catch(string input, Exception exception) => new(File, Program, R.UnhandledException, input, string.Empty, exception.ToString());
            public TestOutputRow Catch(Exception exception) => new(File, Program, R.UnhandledException, string.Empty, string.Empty, exception.ToString());

            static string Stringify(IReadOnlyDictionary<string, object> obj) => JsonSerializer.Serialize(obj);
        }

        public override string ToString() => $"{Result}: in {File} run {Program} with {Input}; expected {Expected}, got {Actual}";
    }
}