using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;


namespace Semgus.CommandLineInterface {
    using R = UnitTestResultCode;

    public record UnitTestOutputRow(string File, string Program, R Result, string Input, string Expected, string Actual) {

        public record Factory(string File, string Program) {
            public UnitTestOutputRow FromStatus(R result, IReadOnlyDictionary<string, object> input, string expected, string actual) => new(File, Program, result, Stringify(input), expected, actual);

            public UnitTestOutputRow FromConstraintCheck(bool expected, bool actual, string report) => new(File, Program, expected == actual ? R.Pass : R.Fail, "<synth-term-constraints>", expected ? "<sat>" : "<unsat>", (actual ? "<sat>:\n" : "<unsat>:\n") + report );

            public UnitTestOutputRow Pass(IReadOnlyDictionary<string, object> input, string expected, string actual) => FromStatus(R.Pass, input, expected, actual);
            public UnitTestOutputRow Fail(IReadOnlyDictionary<string, object> input, string expected, string actual) => FromStatus(R.Fail, input, expected, actual);

            public UnitTestOutputRow Catch(string input, Exception exception) => new(File, Program, R.UnhandledException, input, string.Empty, exception.ToString());
            public UnitTestOutputRow Catch(Exception exception) => new(File, Program, R.UnhandledException, string.Empty, string.Empty, exception.ToString());

            static string Stringify(IReadOnlyDictionary<string, object> obj) => JsonSerializer.Serialize(obj);
        }

        public override string ToString() => $"{Result}: in {File} run {Program} with {Input}; expected {Expected}, got {Actual}";
    }
}