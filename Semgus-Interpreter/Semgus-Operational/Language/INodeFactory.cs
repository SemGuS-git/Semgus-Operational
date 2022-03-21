namespace Semgus.Interpretation {
    public interface INodeFactory {
        IDSLSyntaxNode Instantiate(ProductionRuleInterpreter rule);
        IDSLSyntaxNode Instantiate(ProductionRuleInterpreter rule, IReadOnlyList<IDSLSyntaxNode> subTerms);
    }
}