namespace Semgus.Interpretation {
    public class InterpreterResult {
        public bool HasError { get; }
        public InterpreterErrorInfo Error { get; }
        public object[] Values { get; }

        public InterpreterResult(object[] values) {
            HasError = false;
            Values = values;
        }

        public InterpreterResult(InterpreterErrorInfo error) {
            HasError = true;
            Error = error;
        }
    }
}