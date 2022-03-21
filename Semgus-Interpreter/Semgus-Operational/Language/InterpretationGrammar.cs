using Semgus.Model;
using Semgus.Util;
using System.Text;

namespace Semgus.Interpretation {
    public class InterpretationLibrary {
        private readonly IReadOnlyDictionary<string, ProductionRuleInterpreter> _signatureMap;
        public IReadOnlyList<ProductionRuleInterpreter> Productions { get; }

        public InterpretationLibrary(IReadOnlyList<ProductionRuleInterpreter> productions) {
            Productions = productions;
            _signatureMap = productions.ToDictionary(prod => ToSyntaxKey(prod.TermType,prod.SyntaxConstructor));
        }

        public bool TryFind(SemgusTermType termType, SemgusTermType.Constructor constructor, out ProductionRuleInterpreter prod) => _signatureMap.TryGetValue(ToSyntaxKey(termType, constructor), out prod);

        private static string ToSyntaxKey(SemgusTermType termType, SemgusTermType.Constructor ctor) {
            var sb = new StringBuilder();
            sb.Append(termType.Name.AsString());
            sb.Append(':');
            sb.Append(ctor.Operator.AsString());
            if (ctor.Children.Length > 0) {

                sb.Append('(');
                foreach (var child in ctor.Children) {
                    sb.Append(child.Name.AsString());
                    sb.Append(',');
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

    }



    public class NonterminalProduction {
        public ProductionRuleInterpreter Production { get; }
        public IReadOnlyList<Nonterminal> ChildNonterminals { get; }

        public NonterminalProduction(ProductionRuleInterpreter production, IReadOnlyList<Nonterminal> childNonterminals) {
            Production = production;
            ChildNonterminals = childNonterminals;
        }
    }

    public record Nonterminal(string Name) {
        public override int GetHashCode() => Name.GetHashCode();
    }


    public class InterpretationGrammar {
        public int RuleCount => Productions.ValueCount;

        public IReadOnlyCollection<Nonterminal> Nonterminals => _nonterminals;
        private readonly HashSet<Nonterminal> _nonterminals;

        public DictOfList<Nonterminal, NonterminalProduction> Productions { get; }

        public InterpretationGrammar(DictOfList<Nonterminal, NonterminalProduction> productions) {
            _nonterminals = new(productions.Keys);
            Productions = productions;
        }

        //public static InterpretationGrammar FromProductions(IEnumerable<ProductionGroup> productionGroups, Theory theory) {
        //    var analyzer = new OperationalSemanticsAnalyzer(theory, productionGroups.Select(pr => pr.RelationInstance));

        //    var nonterminalToProductions = new DictOfList<Nonterminal, ProductionRuleInterpreter>();

        //    var productionsBySyntax = new Dictionary<SyntaxConstraint, ProductionRuleInterpreter>();

        //    foreach (var group in productionGroups) {
        //        var relationInstance = group.RelationInstance;
                
        //        foreach (var rule in group.SemanticRules) {
        //            var syntax = SyntaxConstraint.From(group.TermVariable, rule.RewriteExpression);
        //            foreach (var predicate in rule.Predicates) {
        //                var analysisResult = analyzer.AnalyzePredicate(relationInstance, syntax, predicate);

        //                if (!productionsBySyntax.TryGetValue(syntax, out var prodInterpreter)) {
        //                    prodInterpreter = analysisResult.GetProductionIntepreter();
        //                    productionsBySyntax.Add(prodInterpreter.Syntax, prodInterpreter);

        //                    if (prodInterpreter.Syntax.IsLeaf()) {
        //                        nonterminalToProductions.Add(group.Nonterminal, prodInterpreter);
        //                    } else {
        //                        nonterminalToProductions.Add(group.Nonterminal, prodInterpreter);
        //                    }
        //                }

        //                analysisResult.AddSemanticInterpeterTo(prodInterpreter);
        //            }
        //        }
        //    }

        //    return new InterpretationGrammar(theory, nonterminalToProductions);
        //}

        //public static InterpretationGrammar FromAst(SemgusProblem ast, Theory theory) => FromProductions(ast.SynthFun.Productions, theory);

    }
}