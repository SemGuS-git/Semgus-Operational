using System.Text;

namespace Semgus.Operational {
    /// <summary>
    /// Encodes a single production of a nonterminal, e.g. `N ::= (E + A)`.
    /// </summary>
    public class NonterminalProduction {
        public ProductionRuleInterpreter Production { get; } /// TODO: replace with semantics-freeinfo; move semantics to separate construct

        public NtSymbol ParentNonterminal { get; }
        public IReadOnlyList<NtSymbol> ChildNonterminals { get; }

        public NonterminalProduction(ProductionRuleInterpreter production, NtSymbol parentNonterminal, IReadOnlyList<NtSymbol> childNonterminals) {
            Production = production;
            ParentNonterminal = parentNonterminal;
            ChildNonterminals = childNonterminals;
        }

        public bool IsLeaf() => ChildNonterminals.Count == 0;

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(ParentNonterminal.Name);
            sb.Append(" ::= ");

            if(IsLeaf()) {
                sb.Append(Production.SyntaxConstructor.Operator.ToString());
            } else {
                sb.Append('(');
                sb.Append(Production.SyntaxConstructor.Operator.ToString());
                foreach (var arg in ChildNonterminals) {
                    sb.Append(arg.Name);
                    sb.Append(' ');
                }
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}