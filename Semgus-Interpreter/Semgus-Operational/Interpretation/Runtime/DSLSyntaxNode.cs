using System.Text;

namespace Semgus.Operational {
    public class DSLSyntaxNode : IDSLSyntaxNode {
        public class Factory : INodeFactory {
            public static Factory Instance { get; } = new Factory();

            public IDSLSyntaxNode Instantiate(NtSymbol nonterminal, ProductionRuleInterpreter rule) => new DSLSyntaxNode(nonterminal, rule);
            public IDSLSyntaxNode Instantiate(NtSymbol nonterminal, ProductionRuleInterpreter rule, IReadOnlyList<IDSLSyntaxNode> childNodes) => new DSLSyntaxNode(nonterminal, rule, childNodes);
        }

        public NtSymbol Nonterminal { get; }
        public ProductionRuleInterpreter ProductionRule { get; }

        public IReadOnlyList<IDSLSyntaxNode> AddressableTerms { get; }
        public IEnumerable<IDSLSyntaxNode> ChildNodes => AddressableTerms.Skip(1);

        public int Size { get; }
        public int Height { get; }
        public bool CanEvaluate => true;

        public DSLSyntaxNode(NtSymbol nonterminal, ProductionRuleInterpreter interpreter, IReadOnlyList<IDSLSyntaxNode>? childNodes = null) {
            Nonterminal = nonterminal;
            ProductionRule = interpreter;

            var termList = new List<IDSLSyntaxNode>() { this };
            if (childNodes is not null) termList.AddRange(childNodes);
            AddressableTerms = termList;

            if(termList.Count==1) {
                Size = 1;
                Height = 1;
            } else {
                int s = 0, h = 0;
                foreach (var node in ChildNodes) {
                    s += node.Size;
                    h = Math.Max(h, node.Height);
                }
                Size = 1 + s;
                Height = 1 + h;
            }
        }

        public void PrettyPrint(StringBuilder sb) {
            if(AddressableTerms.Count>1) {
                sb.Append('(');
                sb.Append(ProductionRule.SyntaxConstructor.Operator.AsString());
                for(int i = 1; i < AddressableTerms.Count; i++) {
                    sb.Append(' ');
                    AddressableTerms[i].PrettyPrint(sb);
                }
                sb.Append(')');
            } else {
                sb.Append(ProductionRule.SyntaxConstructor.Operator.AsString());
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint(sb);
            return sb.ToString();
        }
    }
}