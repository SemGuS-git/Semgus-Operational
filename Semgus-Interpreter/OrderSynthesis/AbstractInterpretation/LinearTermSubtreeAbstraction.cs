using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class LinearTermSubtreeAbstraction {
        public IReadOnlyList<AbstractTermEvalStep> Steps { get; }

        public LinearTermSubtreeAbstraction(IReadOnlyList<AbstractTermEvalStep> Steps) {
            this.Steps = Steps;
        }

        public bool EvaluateSubtrees(AbstractInterpretationLibrary absSem, IDSLSyntaxNode node, MuxInterval main_input, out MuxInterval[] result) {
            var tuple_values = new MuxInterval[Steps.Count + 1];
            tuple_values[0] = main_input;

            foreach (var step in Steps) {


                if (!absSem.Interpret(
                    node.AddressableTerms[step.NodeTermIndex],
                    tuple_values[step.InputTupleIndex],
                    out tuple_values[step.OutputTupleIndex]
                )) {
                    result = null;
                    return false;
                }
            }

            result = tuple_values;
            return true;
        }
    }
}
