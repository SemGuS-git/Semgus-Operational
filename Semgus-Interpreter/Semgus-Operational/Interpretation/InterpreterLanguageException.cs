namespace Semgus.Interpretation {
    public class InterpreterLanguageException : Exception {
        public IDSLSyntaxNode Node { get; }
        public IReadOnlyDictionary<string, object> InputVariables { get; }

        public InterpreterLanguageException(string message) : base(message) { }

        public InterpreterLanguageException(string message, IDSLSyntaxNode node, IReadOnlyDictionary<string,object> inputVariables) : base(message) {
            this.Node = node;
            this.InputVariables = inputVariables;
        }
    }
}