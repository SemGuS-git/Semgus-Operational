using System.Text;

namespace Semgus.Operational {
    public class EvaluationContext {
        public IDSLSyntaxNode ThisTerm => Terms[0];
        public IReadOnlyList<IDSLSyntaxNode> Terms { get; }
        public object[] Variables { get; }

        public EvaluationContext(IReadOnlyList<IDSLSyntaxNode> terms, object[] variables) {
            Terms = terms;
            Variables = variables;
        }
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append('{');
            if (Terms.Count > 0) {
                Terms[1].PrettyPrint(sb); ;
                for (int i = 2; i < Terms.Count; i++) {
                    sb.Append(", ");
                    Terms[i].PrettyPrint(sb);
                }
                if(Variables.Length>0) {
                    sb.Append("; ");
                }
            }

            if (Variables.Length > 0) {
                sb.Append(Variables[0].ToString());
                for (int i = 1; i < Variables.Length; i++) {
                    sb.Append(", ");
                    sb.Append(Variables[i]?.ToString()??"null");
                }
            }

            sb.Append('}');
            return sb.ToString();
        }
    }
}