using System.Text;

namespace Semgus.Operational {
    public interface IDSLSyntaxNode {
        NtSymbol Nonterminal { get; }
        ProductionRuleInterpreter ProductionRule { get; }
        IReadOnlyList<IDSLSyntaxNode> AddressableTerms { get; }

        int Size { get; }
        int Height { get; }

        bool CanEvaluate { get; } // true iff this program has concrete semantics

        void PrettyPrint(StringBuilder sb);
    }
}
