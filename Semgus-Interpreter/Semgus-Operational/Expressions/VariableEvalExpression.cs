using System.Text;

namespace Semgus.Operational {
    public class VariableEvalExpression : ISmtLibExpression {
        public VariableInfo Variable { get; }
        //public Type ResultType => Variable.Type;

        public VariableEvalExpression(VariableInfo variable) {
            this.Variable = variable;
        }

        public object Evaluate(EvaluationContext context) => context.Variables[Variable.Index];
        public string PrettyPrint() {
            var sb = new StringBuilder();
            PrettyPrint(sb);
            return sb.ToString();
        }

        public void PrettyPrint(StringBuilder sb) {
            sb.Append(Variable.Name);
        }
    }
}