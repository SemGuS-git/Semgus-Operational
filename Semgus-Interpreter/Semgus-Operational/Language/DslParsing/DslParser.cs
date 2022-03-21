#if TODO_IS_DONE
namespace Semgus.Interpretation {
    public class DslParser {
        private readonly IReadOnlyDictionary<string, List<ProductionRuleInterpreter>> _prodDict;
        private readonly INodeFactory _nodeFactory;

        private DslParser(IReadOnlyDictionary<string, List<ProductionRuleInterpreter>> prodDict, INodeFactory nodeFactory) {
            _prodDict = prodDict;
            _nodeFactory = nodeFactory;
        }

        public static DslParser FromGrammar(InterpretationGrammar grammar, INodeFactory factory) {
            var dict = grammar.Productions.Values
                .SelectMany(m => m)
                .GroupBy(m => m.Syntax.Constructor)
                .ToDictionary(g => g.Key, g => g.ToList());

            return new DslParser(dict, factory);
        }

        public IDSLSyntaxNode Parse(string str) => Parse(SExpressionNode.Parse(str));

        // Parse this as *any* nonterminal
        // should only occur at the root node
        public IDSLSyntaxNode Parse(SExpressionNode node) {
            if (!_prodDict.TryGetValue(node.symbol, out var candidates)) {
                throw new KeyNotFoundException(node.symbol);
            }

            if (candidates.Count == 0) {
                throw new ArgumentException($"No production has syn ctor {node.symbol}: {node}");
            }

            var solu = new List<IDSLSyntaxNode>();

            foreach (var candidate in candidates) {
                if (TryParse(node, candidate, out var instance, out var err)) {
                    solu.Add(instance);
                }
            }

            if (solu.Count == 0) {
                throw new ArgumentException($"No valid interpretation of {node}");
            } else if (solu.Count > 1) {
                throw new ArgumentException($"Parsing is ambigouous for {node}");
            }

            return solu[0];
        }

        // Parse this as a specific nonterminal
        public IDSLSyntaxNode Parse(SExpressionNode node, Nonterminal nt) {
            if (!_prodDict.TryGetValue(node.symbol, out var productionsMatchingCtor)) {
                throw new KeyNotFoundException(node.symbol);
            }

            var candidates = productionsMatchingCtor.Where(e => e.Syntax.TermVariable.Nonterminal == nt).ToList();

            if(candidates.Count==0) {
                throw new ArgumentException($"No production of nonterminal {nt} has syn ctor {node.symbol}: {node}");
            }

            var solu = new List<IDSLSyntaxNode>();

            foreach(var candidate in candidates) {
                if (TryParse(node, candidate, out var instance, out var err)) {
                    solu.Add(instance);
                }
            }

            if(solu.Count == 0) {
                throw new ArgumentException($"No valid interpretation of {node}");
            } else if(solu.Count>1) {
                throw new ArgumentException($"Parsing is ambigouous for {node}");
            }

            return solu[0];
        }

        private bool TryParse(SExpressionNode node, ProductionRuleInterpreter prod, out IDSLSyntaxNode instance, out Exception err) { 
            var syn = prod.Syntax;

            int n = node.children.Count;
            if (n != syn.Args.Count) {
                err = new Exception($"Error parsing prod {syn}: incorrect child count in expr {node}");
                instance = default;
                return false;
            }

            List<IDSLSyntaxNode> children = new();

            for (int i = 0; i < n; i++) {
                if (syn.Args[i] is Nonterminal nt) {
                    children.Add(Parse(node.children[i],nt));
                } else {
                    if (node.children[i].children.Count != 0) {
                        err = new Exception(
                            $"Error parsing {i}th arg of prod {syn}: " +
                            $"Expected terminal symbol {syn.Args[i]}, got subexpr {node.children[i]}"
                        );
                        instance = default;
                        return false;
                    }
                    if (node.children[i].symbol != syn.Args[i].ToString()) {
                        err = new Exception(
                            $"Error parsing {i}th arg of prod {syn}: " +
                            $"Expected terminal symbol {syn.Args[i]}, got symbol {node.children[i].symbol}"
                        );
                        instance = default;
                        return false;
                    }
                }
            }


            instance = _nodeFactory.Instantiate(prod, children);
            err = default;
            return true;
        }
    }
}
#endif