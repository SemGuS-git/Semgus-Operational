using System.Text;

namespace Semgus.Interpretation {
    public class InterpreterErrorInfo {
        public Exception InnerException { get; }
        public IInterpretationStep Step { get; }
        public EvaluationContext EvalContext { get; }

        public List<SemanticRuleInterpreter> Trace { get; } = new();

        public InterpreterErrorInfo(Exception innerException, IInterpretationStep step, EvaluationContext context) {
            InnerException = innerException;
            Step = step;
            EvalContext = context;
        }

        public override string ToString() {
            return $"Interpreter runtime error while executing {Step} with state {EvalContext}";
        }

        public string PrettyPrint(bool multiline = false) {
            var sb = new StringBuilder();
            sb.Append(ToString());
            if (multiline) {
                sb.AppendLine();
            }

            foreach (var site in Trace) {
                if (multiline) sb.Append("   ");
                sb.Append($" at {site.ProductionRule}");
                if (multiline) sb.AppendLine();
            }
            if (!multiline) sb.Append(". ");

            sb.Append($"Exception: {InnerException.GetType().Name}({InnerException.Message})");
            return sb.ToString();
        }

        internal Exception ToException() {
            throw new NotImplementedException();
        }
    }
}