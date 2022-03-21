namespace Semgus.Interpretation {
    public class DSLSyntaxNode : IDSLSyntaxNode {
        public class Factory : INodeFactory {
            public static Factory Instance { get; } = new Factory();

            public IDSLSyntaxNode Instantiate(ProductionRuleInterpreter rule) => new DSLSyntaxNode(rule);
            public IDSLSyntaxNode Instantiate(ProductionRuleInterpreter rule, IReadOnlyList<IDSLSyntaxNode> childNodes) => new DSLSyntaxNode(rule, childNodes);
        }

        //public Nonterminal Nonterminal { get; }
        public ProductionRuleInterpreter ProductionRule { get; }

        public IReadOnlyList<IDSLSyntaxNode> AddressableTerms { get; }
        public IEnumerable<IDSLSyntaxNode> ChildNodes => AddressableTerms.Skip(1);

        public int Size { get; }
        public int Height { get; }
        public bool CanEvaluate => true;

        public DSLSyntaxNode(ProductionRuleInterpreter interpreter, IReadOnlyList<IDSLSyntaxNode> childNodes = null) {
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

        //public override string ToString() => ProductionRule.Syntax.PrintSyntaxTree(ChildNodes.ToList()); // todo remove listification
    }
}