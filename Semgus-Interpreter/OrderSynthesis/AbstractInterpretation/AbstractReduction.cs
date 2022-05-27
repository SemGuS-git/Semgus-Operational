using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class AbstractReduction : Solvers.Enumerative.IReduction {
        public ILogger? Logger { get; set; }
        IReadOnlyList<BehaviorExample> Examples { get; }
        AbstractInterpretationLibrary AbsSem { get; }

        public AbstractReduction(IReadOnlyList<BehaviorExample> examples, AbstractInterpretationLibrary absSem) {
            Examples = examples;
            AbsSem = absSem;
        }


        public bool CanPrune(IDSLSyntaxNode node) {
            if (node.CanEvaluate) return false; // no point
            foreach (var example in Examples) {
                if (AbsSem.Prune(node,example.Values)) return true;
            }
            return false;
        }
    }
}
