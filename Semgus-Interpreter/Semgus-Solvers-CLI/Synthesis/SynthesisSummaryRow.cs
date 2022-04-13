using Semgus.Solvers;
using System;

namespace Semgus.CommandLineInterface {
    public class SynthesisSummaryRow {
        public string File { get; set; }
        public string ConfigId { get; set; }
        public string Result { get; set; }
        public string RuntimeSec { get; set; }
        public string Program { get; set; }

        public static SynthesisSummaryRow Convert(ISynthesisResult result) => new SynthesisSummaryRow {
            File = result.InputInfo.ProblemFile,
            ConfigId = result.InputInfo.ConfigIdentifier,
            Result = Stringify(result.StopCode),
            RuntimeSec = $"{result.Runtime.TotalSeconds:0.0000}",
            Program = result.Program?.ToString() ?? "",
        };

        private static string Stringify(SynthesisStopCode result) => result switch {
            SynthesisStopCode.SAT => "sat",
            SynthesisStopCode.UNSAT => "unsat",
            SynthesisStopCode.Timeout => "timeout",
            SynthesisStopCode.Bound => "bound",
            SynthesisStopCode.Error => "error",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
