namespace Semgus.Operational {
    public class InterpreterState {
        public int maxDepth;
        public int recursionDepth;
        public bool HasError { get; private set; }
        public InterpreterErrorInfo? Error { get; private set; } = null;

        public void FlagException(Exception e, IInterpretationStep step, EvaluationContext context) {
            HasError = true;
            Error = new(e,step,context);
        }
    }
}