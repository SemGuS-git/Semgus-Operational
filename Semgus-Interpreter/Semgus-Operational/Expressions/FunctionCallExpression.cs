using System.Text;

namespace Semgus.Operational {
    public class FunctionCallExpression : ISmtLibExpression {
        public FunctionInstance Function { get; }
        public IReadOnlyList<ISmtLibExpression> Args { get; }

        //public Type ResultType => Function.Signature.OutputType;

        public FunctionCallExpression(FunctionInstance function, IReadOnlyList<ISmtLibExpression> args) {
            this.Function = function;
            this.Args = args;
        }

        public object Evaluate(EvaluationContext context) {
            var concreteArgs = new object[Args.Count];
            for (int i = 0; i < Args.Count; i++) {
                concreteArgs[i] = Args[i].Evaluate(context);
            }
            return Function.Evaluate(concreteArgs);
        }

        public string PrettyPrint() {
            var sb = new StringBuilder();
            PrettyPrint(sb);
            return sb.ToString();
        }

        public void PrettyPrint(StringBuilder sb) {
            if(Args.Count>0) {
                sb.Append('(');
                sb.Append(Function.Name);
                for (int i = 0; i < Args.Count; i++) {
                    sb.Append(' ');
                    Args[i].PrettyPrint(sb);
                }
                sb.Append(')');
            } else {
                sb.Append(Function.Name);
            }
        }
    }   
}