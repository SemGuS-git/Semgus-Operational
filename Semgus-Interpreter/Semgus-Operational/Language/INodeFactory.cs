namespace Semgus.Operational {
    public interface INodeFactory {
        IDSLSyntaxNode Instantiate(NtSymbol nonterminal, ProductionRuleInterpreter rule);
        IDSLSyntaxNode Instantiate(NtSymbol nonterminal, ProductionRuleInterpreter rule, IReadOnlyList<IDSLSyntaxNode> subTerms);
    }
}