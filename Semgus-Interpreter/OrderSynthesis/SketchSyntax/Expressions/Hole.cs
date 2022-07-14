using System.Text;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Hole  (int? BitCount = null, string ? Label = null)  : IExpression  {
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("??");
            if(BitCount.HasValue) {
                sb.Append('(');
                sb.Append(BitCount.Value);
                sb.Append(')');
            }
            if(Label is string s) {
                sb.Append(" /*");
                sb.Append(s);
                sb.Append("*/");
            }

            return sb.ToString();
        }
    }
}
