namespace Semgus.Interpretation {
    public interface IDSLSyntaxNode {
        //Nonterminal Nonterminal { get; }
        ProductionRuleInterpreter ProductionRule { get; }
        IReadOnlyList<IDSLSyntaxNode> AddressableTerms { get; }

        int Size { get; }
        int Height { get; }

        bool CanEvaluate { get; } // true iff this program has concrete semantics
    }
}
