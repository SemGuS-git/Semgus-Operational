using System.Text;

namespace Semgus.Interpretation {
    /// <summary>
    /// Evaluate some child term on a collection of variables.
    /// </summary>
    public class TermEvaluation : IInterpretationStep {
        public TermVariableInfo Term { get; }
        public IReadOnlyList<VariableInfo> InputVariables { get; }
        public IReadOnlyList<VariableInfo> OutputVariables { get; }
        
        private readonly int _n_in;
        private readonly int _n_out;
        private readonly int _n;

        public TermEvaluation(TermVariableInfo term, IReadOnlyList<VariableInfo> inputVariables, IReadOnlyList<VariableInfo> outputVariables) {
            Term = term;
            InputVariables = inputVariables;
            OutputVariables = outputVariables;
            _n_in = InputVariables.Count;
            _n_out = OutputVariables.Count;
            _n = _n_in + _n_out;
        }

        public bool Execute(EvaluationContext context, InterpreterState state) {
            var tNode = context.Terms[Term.Index];

            // Note: in general, the elements of this semantic relation instance could be arbitrary SMT-LIB2 formulas.
            // For now, we constrain them to be plain variable references.

            var nextVariables = new object[_n + tNode.ProductionRule.ScratchSize];

            for(int i = 0; i < _n_in;i++) {
                nextVariables[i] = context.Variables[InputVariables[i].Index]; // Map inputs
            }

            // Increment recursion depth, check whether exceeds bounds
            if ((state.recursionDepth += 1) > state.maxDepth) {
                state.FlagException(new RecursionDepthException(state.recursionDepth), this, context);
                state.recursionDepth -= 1;
                return false;
            }

            var next = new EvaluationContext(tNode.AddressableTerms, nextVariables);
            tNode.ProductionRule.Interpret(next, state);

            state.recursionDepth -= 1;

            for (int i = 0; i < _n_out; i++) {
                // Write output values to parent context
                context.Variables[OutputVariables[i].Index] = nextVariables[i+_n_in]; 
            }


            return true;
        }

        public string PrintCode() {
            var names = new string[_n];
            foreach (var v in InputVariables) { names[v.Index] = v.Name; }
            foreach (var v in OutputVariables) { names[v.Index] = "out " + v.Name; }

            var sb = new StringBuilder();
            sb.Append("Eval ");
            sb.Append(Term.Name);
            sb.Append(" (");
            if (_n > 0) {
                for (int i = 0; i < _n - 1; i++) {
                    sb.Append(names[i]);
                    sb.Append(", ");
                }
                sb.Append(names[_n - 1]);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString() => PrintCode();
    }
}