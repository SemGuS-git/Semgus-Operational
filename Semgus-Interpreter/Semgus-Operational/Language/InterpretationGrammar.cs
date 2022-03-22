using Semgus.Model;
using Semgus.Util;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Semgus.Operational {
    public class InterpretationLibrary {
        private readonly IReadOnlyDictionary<string, ProductionRuleInterpreter> _signatureMap;

        public RelationTracker Relations { get; }
        public IReadOnlyList<ProductionRuleInterpreter> Productions { get; }

        public InterpretationLibrary(RelationTracker relations, IReadOnlyList<ProductionRuleInterpreter> productions) {
            Relations = relations;
            Productions = productions;
            _signatureMap = productions.ToDictionary(prod => ToSyntaxKey(prod.TermType,prod.SyntaxConstructor));
        }

        public bool TryFind(SemgusTermType termType, SemgusTermType.Constructor constructor, [NotNullWhen(true)] out ProductionRuleInterpreter? prod) => _signatureMap.TryGetValue(ToSyntaxKey(termType, constructor), out prod);

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

    public record NtSymbol(string Name) {
        public override int GetHashCode() => Name.GetHashCode();
    }


    public class InterpretationGrammar {
        public int RuleCount => Productions.ValueCount;

        public IReadOnlyCollection<NtSymbol> Nonterminals => _nonterminals;
        private readonly HashSet<NtSymbol> _nonterminals;

        public DictOfList<NtSymbol, NonterminalProduction> Productions { get; }

        public InterpretationGrammar(DictOfList<NtSymbol, NonterminalProduction> productions) {
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