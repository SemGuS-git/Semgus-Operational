using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {

    internal class AbstractInterpretationLibrary {
        IReadOnlyList<LinearAbstractSemantics> IndexedSemantics { get; } // Indexed according to production index
        IReadOnlyDictionary<string, (MuxTupleType input, MuxTupleType output)> TypesByTermType { get; }
        public IReadOnlyDictionary<NtSymbol, MuxInterval> HoleIntervals { get; }

        public AbstractInterpretationLibrary(IReadOnlyList<LinearAbstractSemantics> indexedSemantics, IReadOnlyDictionary<string, (MuxTupleType input, MuxTupleType output)> typesByTermType, IReadOnlyDictionary<NtSymbol, MuxInterval> holeIntervals) {
            IndexedSemantics = indexedSemantics;
            TypesByTermType = typesByTermType;
            HoleIntervals = holeIntervals;
        }

        public bool Prune(IDSLSyntaxNode node, object[] argValues) {
            var n = node.ProductionRule.InputVariables.Count;

            var inputs = new object[n];

            if (argValues.Length < n) throw new Exception();

            for (int i = 0; i < n; i++) {
                var j = node.ProductionRule.InputVariables[i].Index;
                inputs[i] = argValues[j];
            }

            var (ty_in, ty_out) = TypesByTermType[node.ProductionRule.TermType.Name.Name.Symbol];

            var ival = ty_in.Instantiate(inputs);

            var ok = Interpret(node, new(ival), out var result);

            if (!ok) return false; // semantics did not hold; doesn't mean we can prune

            var m = node.ProductionRule.OutputVariables.Count;
            var ex_val = new object[m];

            for (int i = 0; i < m; i++) {
                var j = node.ProductionRule.OutputVariables[i].Index;
                ex_val[i] = argValues[j];
            }

            var expected = ty_out.Instantiate(ex_val);

            if(result.DoesNotContain(expected)) {
                return true;
            } else {
                return false;
            }
        }

        internal bool Interpret(IDSLSyntaxNode target, MuxInterval main_input, out MuxInterval main_output)
            => (
                target is Solvers.Enumerative.Hole
                && HoleIntervals.TryGetValue(target.Nonterminal, out main_output!)
            )
            || IndexedSemantics[target.ProductionRule.SequenceNumber].Interpret(this, target, main_input, out main_output);
    }
}
