using System.Text;
using Semgus;

namespace Semgus.Operational {
    /// <summary>
    /// Evaluate some child term on a collection of variables.
    /// </summary>
    public class TermEvaluation : IInterpretationStep {
        public TermVariableInfo Term { get; }

        private readonly List<(bool isOutput, VariableInfo info)> _args;
        public IReadOnlyList<(bool isOutput, VariableInfo info)> Args => _args;

        public IReadOnlyList<VariableInfo> InputVariables { get; }
        public IReadOnlyList<VariableInfo> OutputVariables { get; }
        
        private readonly int _n_in;
        private readonly int _n_out;
        private readonly int _n;

        public TermEvaluation(TermVariableInfo term, List<(bool isOutput, VariableInfo info)> args) {

            Term = term;
            _args = args;

            List<VariableInfo> inputs = new(), outputs = new();
            foreach (var tu in args) {
                (tu.isOutput ? outputs : inputs).Add(tu.info);
            }
            InputVariables = inputs;
            OutputVariables = outputs;
            _n_in = inputs.Count;
            _n_out = outputs.Count;
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
            var sb = new StringBuilder();
            sb.Append("Eval ");
            sb.Append(Term.Name);
            sb.Append(" (");
            if (_n > 0) {
                if (_args[0].isOutput) sb.Append("out ");
                sb.Append(_args[0].info.Name);
                for (int i = 1; i < _n; i++) {
                    sb.Append(", ");
                    if (_args[i].isOutput) sb.Append("out ");
                    sb.Append(_args[i].info.Name);
                }
            }
            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString() => PrintCode();
    }
}