using System.Text;

namespace Semgus.OrderSynthesis {
    internal class StringLineReceiver : ILineReceiver {
        const string INDENT = "    ";
        readonly StringBuilder sb = new();
        int indentLevel = 0;
        readonly bool compact;

        public StringLineReceiver(StringBuilder sb, bool compact=false) {
            this.sb = sb;
            this.compact = compact;
            this.indentLevel = indentLevel;
        }

        public void Add(string line) {
            if (compact) {
                sb.Append(line);
                sb.Append(' ');
            } else {
                for (int i = 0; i < indentLevel; i++) sb.Append(INDENT);
                sb.AppendLine(line);
            }
        }

        public void IndentIn() => indentLevel++;
        public void IndentOut() => indentLevel--;
    }
}