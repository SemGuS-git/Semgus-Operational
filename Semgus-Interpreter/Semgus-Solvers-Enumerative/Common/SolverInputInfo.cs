using System;

namespace Semgus.Solvers {
    public class SolverInputInfo {
        public DateTime StartTime { get; }
        public string ProblemFile { get; }
        public string BatchFile { get; }

        public SolverInputInfo(DateTime startTime, string problemFile, string batchFile) {
            StartTime = startTime;
            ProblemFile = problemFile;
            BatchFile = batchFile;
        }
    }

}
