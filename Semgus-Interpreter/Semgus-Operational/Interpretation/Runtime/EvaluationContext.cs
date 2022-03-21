namespace Semgus.Interpretation {
    public class EvaluationContext {
        public IDSLSyntaxNode ThisTerm => Terms[0];
        public IReadOnlyList<IDSLSyntaxNode> Terms { get; }
        public object[] Variables { get; }

        public EvaluationContext(IReadOnlyList<IDSLSyntaxNode> terms, object[] variables) {
            Terms = terms;
            Variables = variables;
        }
    }
}