using Semgus.Operational;
using System;

namespace Semgus.Solvers {
    public class FileParseErrorResult : ISynthesisResult {
        public SolverInputInfo InputInfo { get; set; }
        public SynthesisStopCode StopCode => SynthesisStopCode.Error;
        public IDSLSyntaxNode Program => null;

        public string ErrorType { get; }
        public string ErrorMessage { get; }

        public TimeSpan Runtime => TimeSpan.Zero;

        public FileParseErrorResult(Exception e) {
            ErrorType = e.GetType().Name;
            ErrorMessage = e.Message;
        }
    }
}
