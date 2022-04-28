using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class AbstractInterpretationLibrary {
        IReadOnlyList<LinearAbstractSemantics> IndexedSemantics { get; } // Indexed according to production index
        IReadOnlyDictionary<NtSymbol, MuxInterval> HoleIntervals { get; }

        public AbstractInterpretationLibrary(IReadOnlyList<LinearAbstractSemantics> indexedSemantics, IReadOnlyDictionary<NtSymbol, MuxInterval> holeIntervals) {
            IndexedSemantics = indexedSemantics;
            HoleIntervals = holeIntervals;
        }

        internal bool Interpret(IDSLSyntaxNode target, MuxInterval main_input, out MuxInterval main_output)
            => (
                target is Solvers.Enumerative.Hole
                && HoleIntervals.TryGetValue(target.Nonterminal, out main_output!)
            )
            || IndexedSemantics[target.ProductionRule.SequenceNumber].Interpret(this, target, main_input, out main_output);
    }
}
