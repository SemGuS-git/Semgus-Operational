namespace Semgus.Operational {
    public class InterpreterRuntimeException : Exception {
        public IDSLSyntaxNode Node { get; }
        public IReadOnlyList<object?> Arguments { get; }

        public InterpreterRuntimeException(Exception innerException, IDSLSyntaxNode node, IReadOnlyList<VariableReference> args) : base(null,innerException) {
            this.Node = node;
            this.Arguments = args.Select(a => a.HasValue ? a.Value : null).ToList();
        }
    }
}