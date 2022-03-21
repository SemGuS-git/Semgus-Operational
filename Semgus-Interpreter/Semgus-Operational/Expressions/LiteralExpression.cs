using System.Text;

namespace Semgus.Interpretation {
    public class LiteralExpression : ISmtLibExpression {
        //public Type ResultType { get; }
        public object BoxedValue { get; }

        public LiteralExpression(object boxedValue) : this(boxedValue, boxedValue.GetType()) { }

        public LiteralExpression(object boxedValue, Type valueType) {
            //this.ResultType = valueType;
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