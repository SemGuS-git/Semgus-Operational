using System;

namespace Semgus.Solvers {
    public class SolverInputInfo {
        public DateTime StartTime { get; }
        public string ProblemFile { get; }

        public string ConfigIdentifier { get; }
        public string BatchLabel { get; }

        public SolverInputInfo(DateTime startTime, string problemFile, string configIdentifier, string batchLabel) {
            StartTime = startTime;
            ProblemFile = problemFile;
            ConfigIdentifier = configIdentifier;
            BatchLabel = batchLabel;
        }
    }

}
