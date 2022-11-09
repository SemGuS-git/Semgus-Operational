using Semgus.Model;
using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Semgus.Operational {
    /// <summary>
    /// A collection of productions for a particular set of term types.
    /// 
    /// This structure does not encode grammar information.
    /// </summary>
    public class InterpretationLibrary {
        private readonly IReadOnlyDictionary<string, ProductionRuleInterpreter> _signatureMap;

        public ITheoryImplementation Theory { get; }
        public RelationTracker SemanticRelations { get; }
        public IReadOnlyList<ProductionRuleInterpreter> Productions { get; }
        public IReadOnlyList<SemgusTermType> TermTypes { get; }

        public InterpretationLibrary(ITheoryImplementation theory, RelationTracker relations, IReadOnlyList<ProductionRuleInterpreter> productions) {
            Theory = theory;
            SemanticRelations = relations;
            Productions = productions;
            _signatureMap = productions.ToDictionary(prod => ToSyntaxKey((SemgusTermType)prod.TermType,prod.SyntaxConstructor));

            Dictionary<string, SemgusTermType> temp = new();
            foreach(var prod in productions) {
                temp.TryAdd(prod.TermType.Name.AsString(), (SemgusTermType)prod.TermType);
            }
            TermTypes = new List<SemgusTermType>(temp.Values);
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
}