namespace Semgus.Interpretation {
    public class SemanticRuleInterpreter {
        public ProductionRuleInterpreter ProductionRule { get; }

        // Variables that will be declared in the scope of this rule during interpretation.
        // The order of this list is arbitrary. If no aux variables are declared, this list will be non-null and empty.
        public IReadOnlyList<VariableInfo> AuxiliaryVariables { get; private set; }

        // Imperative steps to perform when interpreting this rule.
        public IReadOnlyList<IInterpretationStep> Steps { get; }

        public SemanticRuleInterpreter(
            ProductionRuleInterpreter productionRule,
            IReadOnlyList<VariableInfo> auxVariables,
            IReadOnlyList<IInterpretationStep> steps
        ) {
            this.ProductionRule = productionRule;
            this.AuxiliaryVariables = auxVariables;
            this.Steps = steps;
        }

        // Interpret a syntax node.
        // Results are stored by mutating the contents of the variables array.
        public bool TryInterpret(EvaluationContext context, InterpreterState state) {
            foreach (var step in Steps) {
                var result = step.Execute(context, state);

                // Each step may raise an error when evaluated
                if (state.HasError) {
                    state.Error.Trace.Add(this);
                    return false;
                }
                if (!result) return false;
            }
            return true;
        }

        // unused?
        public IEnumerable<ConditionalAssertion> GetPreconditions() {
            foreach (var step in Steps) {
                if (step is ConditionalAssertion assertion) yield return assertion;
                else yield break;
            }
        }

        public override string ToString() => string.Join(";\n", Steps.Select(s => s.PrintCode()));
    }
}