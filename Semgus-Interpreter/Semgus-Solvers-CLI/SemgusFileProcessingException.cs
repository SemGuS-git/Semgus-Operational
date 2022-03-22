using System;

namespace Semgus.CommandLineInterface {
    [Serializable]
    public class SemgusFileProcessingException : Exception {
        public string ProblemFileName { get; }

        public SemgusFileProcessingException(string problemFileName, Exception innerException) : base($"Error of type {innerException.GetType().Name} occurred while processing {problemFileName}", innerException) {
            this.ProblemFileName = problemFileName;
        }
    }
}