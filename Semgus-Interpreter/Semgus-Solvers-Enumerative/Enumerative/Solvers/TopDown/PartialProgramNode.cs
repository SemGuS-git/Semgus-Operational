using Semgus.Operational;
using System;
using System.Collections.Generic;
using System.Text;

namespace Semgus.Solvers.Enumerative {
    public class PartialProgramNode : IPartialProgram {
        public NtSymbol Nonterminal { get; }
        public ProductionRuleInterpreter ProductionRule { get; }
        

        public IReadOnlyList<IDSLSyntaxNode> AddressableTerms => throw new InvalidOperationException();
        public IEnumerable<IDSLSyntaxNode> ChildNodes => _childNodes;

        public int Size { get; init; }
        public int Height => throw new NotImplementedException();
        public bool CanEvaluate => false;

        public PartialProgramNode Parent { get; private set; } // may be null

        private readonly IDSLSyntaxNode[] _childNodes;
        private readonly int _index;

        public static PartialProgramNode FromRule(NonterminalProduction template, PartialProgramNode parent, int index, Dictionary<NtSymbol,int> ntCosts) {
            int n = template.ChildNonterminals.Count;
            if (n == 0) throw new ArgumentException("Partial program cannot be a leaf node");
            
            var ch = new Hole[n];
            for(int i = 0; i < n; i++) {
                var nt = template.ChildNonterminals[i];
                ch[i] = new Hole(nt, i, ntCosts[nt]);
            }

            var node = new PartialProgramNode(template.ParentNonterminal, template.Production, ch, index);
            node.SetParent(parent);
            return node;
        }

        public PartialProgramNode(NtSymbol nonterminal, ProductionRuleInterpreter interpreter, IDSLSyntaxNode[] childNodes, int index) {
            if (childNodes is null || childNodes.Length == 0) throw new ArgumentException("Partial program cannot be a leaf node");
            this.Nonterminal = nonterminal;

            ProductionRule = interpreter;
            _childNodes = childNodes;
            _index = index;

            int s = 0;//, h = 0;
            for (int i = 0; i < childNodes.Length; i++) {
                var node = childNodes[i];
                if (node is IPartialProgram a) a.SetParent(this);
                s += node.Size;
                //h = Math.Max(h, node.Height);
            }
            Size = 1 + s;
            //Height = 1 + h;
        }

        public void SetParent(PartialProgramNode parent) {
            if (this.Parent is not null) throw new InvalidOperationException("Node already has a parent");
            this.Parent = parent;
        }

        public IDSLSyntaxNode UpwardSubstituteClone(int slotIndex, IDSLSyntaxNode slotSubstitute) {
            var ch = new IDSLSyntaxNode[_childNodes.Length];

            var isGround = slotSubstitute is not IPartialProgram;

            for(int i = 0; i < _childNodes.Length;i++) {
                if(i==slotIndex) {
                    ch[i] = slotSubstitute;
                } else if(_childNodes[i] is IPartialProgram partial) {
                    ch[i] = partial.DownClone();
                    isGround = false;
                } else {
                    ch[i] = _childNodes[i];
                }
            }

            IDSLSyntaxNode node = isGround ? new DSLSyntaxNode(Nonterminal, ProductionRule, ch) : new PartialProgramNode(Nonterminal, ProductionRule, ch, _index);
            return (Parent is null) ? node : Parent.UpwardSubstituteClone(_index, node);
        }

        public IPartialProgram DownClone() {
            var ch = new IDSLSyntaxNode[_childNodes.Length];
            for (int i = 0; i < _childNodes.Length; i++) {
                if (_childNodes[i] is IPartialProgram partial) {
                    ch[i] = partial.DownClone();
                } else {
                    ch[i] = _childNodes[i];
                }
            }
            return new PartialProgramNode(Nonterminal, ProductionRule, ch, _index);
        }


        public Hole GetFirstHole() {
            // Traverse tree breadth-first
            Queue<PartialProgramNode> queue = new();
            queue.Enqueue(this);
            while (queue.Count > 0) {
                var next = queue.Dequeue();
                foreach (var child in next.ChildNodes) {
                    switch (child) {
                        case Hole hole:
                            return hole;
                        case PartialProgramNode partial:
                            queue.Enqueue(partial);
                            continue;
                    }
                }
            }

            throw new Exception(); // Invalid state: non-ground program must contain a hole in its tree
        }

        public void PrettyPrint(StringBuilder sb) {
            if (AddressableTerms.Count > 1) {
                sb.Append('(');
                sb.Append(ProductionRule.SyntaxConstructor.Operator.AsString());
                for (int i = 1; i < AddressableTerms.Count; i++) {
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