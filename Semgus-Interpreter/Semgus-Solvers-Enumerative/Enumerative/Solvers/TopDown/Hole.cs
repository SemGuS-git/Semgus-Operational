using Semgus.Operational;
using System;
using System.Collections.Generic;
using System.Text;

namespace Semgus.Solvers.Enumerative {
    public class Hole : IPartialProgram {
        public NtSymbol Nonterminal { get; }
        public PartialProgramNode Parent { get; private set; }
        private readonly int _index;
        private readonly int _cost;

        public IEnumerable<IDSLSyntaxNode> ChildNodes => null;
        public ProductionRuleInterpreter ProductionRule => throw new InvalidOperationException();
        public IReadOnlyList<IDSLSyntaxNode> AddressableTerms { get; } = Array.Empty<IDSLSyntaxNode>();

        public int Size => _cost;
        public int Height => throw new NotImplementedException();
        public bool CanEvaluate => false;

        public Hole(NtSymbol nonterminal, int index, int cost) {
            Nonterminal = nonterminal;
            this._index = index;
            this._cost = cost;
        }

        public void SetParent(PartialProgramNode parent) {
            if (this.Parent is not null) throw new InvalidOperationException("Node already has a parent");
            this.Parent = parent;
        }


        // Returns the *root* node of the AST.
        public IDSLSyntaxNode ReplaceWith(NonterminalProduction template) => Parent.UpwardSubstituteClone(_index, MakeReplacement(template));

        private IDSLSyntaxNode MakeReplacement(NonterminalProduction template) {
            int n = template.ChildNonterminals.Count;
            if (n == 0) return new DSLSyntaxNode(template.ParentNonterminal, template.Production);

            var ch = new Hole[n];
            for (int i = 0; i < n; i++) {
                ch[i] = new Hole(template.ChildNonterminals[i], i, _cost);
            }

            return new PartialProgramNode(template.ParentNonterminal, template.Production, ch, _index);
        }

        public IPartialProgram DownClone() => new Hole(Nonterminal, _index, _cost);

        public void PrettyPrint(StringBuilder sb) {
            sb.Append("??");
            sb.Append(Nonterminal.Name);
        }

        public override string ToString() => Nonterminal.Name;
    }
}