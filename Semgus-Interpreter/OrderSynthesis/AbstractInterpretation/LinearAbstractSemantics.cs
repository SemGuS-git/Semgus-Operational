using Semgus.MiniParser;
using Semgus.Model;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.Subproblems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.AbstractInterpretation {

    // Abstract semantics for a formulaic term
    internal class LinearAbstractSemantics {
        private readonly LinearTermSubtreeAbstraction subtrees;
        private readonly ConcreteTransformer concreteTransformer;
        private readonly IReadOnlyList<Monotonicity> monotonicities;

        public LinearAbstractSemantics(LinearTermSubtreeAbstraction subtrees, ConcreteTransformer concreteTransformer, IReadOnlyList<Monotonicity> monotonicities) {
            this.subtrees = subtrees;
            this.concreteTransformer = concreteTransformer;
            this.monotonicities = monotonicities;
        }

        public bool Interpret(AbstractInterpretationLibrary context, IDSLSyntaxNode node, MuxInterval main_input, out MuxInterval main_output) {

            var ok = subtrees.EvaluateSubtrees(context, node, main_input, out var subtree_output_tuples);

            if (!ok) {
                main_output = default;
                return false;
            }

            List<MuxTuple> left = new(), right = new();

            for (int i = 0; i < monotonicities.Count; i++) {
                var itv = subtree_output_tuples[i];
                switch (monotonicities[i]) {
                    case Monotonicity.Increasing:
                        left.Add(itv.Left);
                        right.Add(itv.Right);
                        break;
                    case Monotonicity.Decreasing:
                        left.Add(itv.Right);
                        right.Add(itv.Left);
                        break;
                    case Monotonicity.None:
                        if (!itv.IsSingle) throw new NotImplementedException(); // todo branch
                        left.Add(itv.Left);
                        right.Add(itv.Left);
                        break;
                }
            }

            var leftResult = concreteTransformer.Evaluate(left);
            var rightResult = concreteTransformer.Evaluate(right);

            main_output = new MuxInterval(leftResult, rightResult);
            return true;
        }
    }
}
