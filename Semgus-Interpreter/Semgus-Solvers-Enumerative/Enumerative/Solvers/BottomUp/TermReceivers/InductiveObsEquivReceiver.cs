using Semgus.Constraints;
using Semgus.Operational;
using Semgus.Util;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Solvers.Enumerative {
    public class InductiveObsEquivReceiver : ITermReceiver {
        private readonly InterpreterHost _interpreter;
        private readonly InductiveConstraint _checker;
        private readonly IReadOnlyList<IReduction> _reductions;

        private readonly ObservationalEquivalenceCache _obsEquivCache = new();

        public InductiveObsEquivReceiver(InterpreterHost interpreter, InductiveConstraint checker, IEnumerable<IReduction> reductions = null) {
            _interpreter = interpreter;
            _checker = checker;
            _reductions = reductions?.ToList() ?? new();
        }

        public TermReceiverCode Receive(IDSLSyntaxNode node) {
            foreach (var reduction in _reductions) {
                if (reduction.CanPrune(node)) return TermReceiverCode.Prune;
            }

            // Don't attempt to evaluate partial programs
            if (!node.CanEvaluate) return TermReceiverCode.Retain;
            
            // If this node is not a production of the root NT, it cannot satisfy the constraint
            bool sat = _checker.MatchesNT(node);
            
            var outputSequence = new Flatten<object>();

            for (int i = 0; i < _checker.ExampleCount; i++) {
                var result_i = _interpreter.RunProgram(node, _checker.Examples[i].Values);

                if (result_i.HasError) {
                    outputSequence.Add(ErrorUnit.InArray);
                    sat = false;
                } else {
                    // Extract values corresponding to *this node's* output variables
                    var outputValues = node.ExtractOutputValues(result_i.Values);
                    outputSequence.Add(outputValues);

                    // Only compare with specification output if we might still be sat
                    if (sat) {
                        // We are safe to check the output values directly from the array, since the nonterminals match
                        sat &= _checker.TestMatchRaw(result_i.Values, i);
                    }
                }

            }

            if (sat) return TermReceiverCode.ReturnSolution;

            var ntCache = _obsEquivCache.SafeGet(node.ProductionRule.TermType.Name.Symbol); // TODO: handle general name cases
            return ntCache.TryAdd(outputSequence, node) ? TermReceiverCode.Retain : TermReceiverCode.Prune;
        }
    }
}
