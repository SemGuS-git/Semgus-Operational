using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Util;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Semgus.Operational {
    public class InterpretationLibrary {
        private readonly IReadOnlyDictionary<string, ProductionRuleInterpreter> _signatureMap;

        public ITheoryImplementation Theory { get; }
        public RelationTracker Relations { get; }
        public IReadOnlyList<ProductionRuleInterpreter> Productions { get; }

        public InterpretationLibrary(ITheoryImplementation theory, RelationTracker relations, IReadOnlyList<ProductionRuleInterpreter> productions) {
            Theory = theory;
            Relations = relations;
            Productions = productions;
            _signatureMap = productions.ToDictionary(prod => ToSyntaxKey(prod.TermType,prod.SyntaxConstructor));
        }

        public bool TryFind(SemgusTermType termType, SemgusTermType.Constructor constructor, [NotNullWhen(true)] out ProductionRuleInterpreter? prod) => _signatureMap.TryGetValue(ToSyntaxKey(termType, constructor), out prod);

        private IEnumerable<ProductionRuleInterpreter> FindByConstructor(SemgusTermType? termType, SmtIdentifier id,  int arity) => Productions.Where(
            prod => (termType is null || prod.TermType.Name == termType.Name) && 
            prod.SyntaxConstructor.Operator == id && 
            prod.SyntaxConstructor.Children.Length == arity
        );

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

        private static readonly NtSymbol MANUAL_NT = new(":manual");

        public IDSLSyntaxNode ParseAST(SmtAttributeValue node, SemgusTermType? termType = null) {
            switch (node.Type) {
                case SmtAttributeValue.AttributeType.Identifier:
                    return new DSLSyntaxNode(MANUAL_NT, FindByConstructor(termType, node.IdentifierValue!, 0).Single());

                case SmtAttributeValue.AttributeType.List:
                    var list = node.ListValue!;
                    
                    var head = list[0];
                    if (head.Type != SmtAttributeValue.AttributeType.Identifier) {
                        throw new ArgumentException();
                    }

                    var prod0 = Productions.Where(prod => prod.SyntaxConstructor.Operator == head.IdentifierValue!).ToList();

                    var prod = FindByConstructor(termType, head.IdentifierValue!, list.Count - 1).Single();
                    var ch = new List<IDSLSyntaxNode>();

                    for(int i = 1; i < list.Count;i++) {
                        ch.Add(ParseAST(list[i], (SemgusTermType)prod.SyntaxConstructor.Children[i-1]));
                    }

                    return new DSLSyntaxNode(MANUAL_NT, prod, ch);
                default:
                    throw new ArgumentException();
            }
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