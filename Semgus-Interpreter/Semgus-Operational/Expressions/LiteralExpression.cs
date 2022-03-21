using Semgus.Model.Smt;
using System.Text;

namespace Semgus.Interpretation {
    public class LiteralExpression : ISmtLibExpression {
        public SmtSort Sort { get; }
        public object BoxedValue { get; }

        public LiteralExpression(object boxedValue, SmtSort sort) {
            this.Sort = sort;
            this.BoxedValue = boxedValue;
        }

        public object Evaluate(EvaluationContext context) => BoxedValue;
        public string PrettyPrint() {
            var sb = new StringBuilder();
            PrettyPrint(sb);
            return sb.ToString();
        }

        public void PrettyPrint(StringBuilder sb) => sb.Append(BoxedValue.ToString());
    }
}