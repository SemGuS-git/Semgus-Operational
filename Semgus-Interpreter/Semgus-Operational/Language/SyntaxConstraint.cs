//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Semgus.Syntax;

//namespace Semgus.Interpretation {
//    public class SyntaxConstraint : IEquatable<SyntaxConstraint> {
//        public TermVariableInfo TermVariable { get; }
//        public string Constructor { get; }
//        public IReadOnlyList<object> Args { get; }
//        public IReadOnlyList<TermVariableInfo> ChildTerms { get; }

//        private SyntaxConstraint(NonterminalTermDeclaration termVariable, string constructor, IReadOnlyList<IProductionRewriteAtom> atoms) {
//            this.TermVariable = new TermVariableInfo(termVariable.Name, 0, termVariable.Nonterminal);
//            this.Constructor = constructor;
//            (this.Args,this.ChildTerms) = ProcessOperands(atoms);
//        }

//        public override bool Equals(object obj) => Equals(obj as SyntaxConstraint);

//        public bool Equals(SyntaxConstraint other) {
//            if (other is null) return false;
//            if (TermVariable.Nonterminal != other.TermVariable.Nonterminal) return false; // todo possibly group by term type instead
//            if (Constructor != other.Constructor) return false;
//            return Args.SequenceEqual(other.Args);
//        }

//        public override int GetHashCode() {
//            var hc = HashCode.Combine(TermVariable, Constructor);
//            foreach (var arg in Args) hc = HashCode.Combine(hc, arg);
//            return hc;
//        }

//        public static bool operator ==(SyntaxConstraint left, SyntaxConstraint right) => EqualityComparer<SyntaxConstraint>.Default.Equals(left, right);
//        public static bool operator !=(SyntaxConstraint left, SyntaxConstraint right) => !(left == right);

//        public bool IsLeaf() => ChildTerms.Count == 0;

//        public override string ToString() => $"{TermVariable.Nonterminal} ::= {Constructor} {string.Join(' ', Args)}";

//        public static SyntaxConstraint From(NonterminalTermDeclaration termVariable, IProductionRewriteExpression rewrite) => rewrite switch {
//            AtomicRewriteExpression e => new SyntaxConstraint(termVariable, GetLeafText(e.Atom), Array.Empty<IProductionRewriteAtom>()),
//            OpRewriteExpression e => new SyntaxConstraint(termVariable, e.Op.Text, e.Operands),
//            _ => throw new NotImplementedException(),
//        };

//        private static string GetLeafText(IProductionRewriteAtom atom) {
//            if (atom is LeafTerm leaf) return leaf.Text;
//            throw new ArgumentException();
//        }

//        private static (List<object> args, List<TermVariableInfo> childTerms) ProcessOperands(IReadOnlyList<IProductionRewriteAtom> atoms) {
//            var argList = new List<object>();
//            var childTerms = new List<TermVariableInfo>();
//            var termNameSet = new HashSet<string>();
//            foreach (var atom in atoms) {
//                switch (atom) {
//                    case LiteralBase literal:
//                        argList.Add(literal.BoxedValue);
//                        break;

//                    case NonterminalTermDeclaration nttd:
//                        argList.Add(nttd.Nonterminal);
//                        childTerms.Add(new TermVariableInfo(nttd.Name, childTerms.Count + 1, nttd.Nonterminal));
//                        if (!termNameSet.Add(nttd.Name)) throw new ArgumentException("The same term may not appear multiple times in a syntax expression");
//                        break;

//                    case LeafTerm leaf:
//                        throw new ArgumentException("Leaf term may not appear as syntax operand");
//                        break;
//                }
//            }
//            return (argList, childTerms);
//        }

//        public string PrintSyntaxTree(IReadOnlyList<IDSLSyntaxNode> childTermInstances) {
//            if (childTermInstances.Count != ChildTerms.Count) throw new ArgumentException();

//            if (Args.Count == 0) return Constructor;

//            int i = 0;

//            string StringifyArg(object obj) {
//                if (obj is Nonterminal nt) {
//                    var node = childTermInstances[i];
//                    if (node.Nonterminal != nt) throw new ArgumentException();
//                    i++;
//                    return node.ToString();
//                } else {
//                    return obj.ToString();
//                }
//            }

//            if (Args.Count == 1 && Constructor == string.Empty) {
//                return StringifyArg(Args[0]);
//            }

//            var sb = new StringBuilder();
//            sb.Append('(');
//            sb.Append(Constructor);
//            foreach (var arg in Args) {
//                sb.Append(' ');
//                sb.Append(StringifyArg(arg));
//            }
//            sb.Append(')');
//            return sb.ToString();
//        }
//    }
//}