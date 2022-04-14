namespace Semgus.OrderSynthesis {
    internal class LineReceiver : ILineReceiver {
        const string INDENT = "    ";
        readonly StreamWriter writer;
        int indentLevel = 0;

        public LineReceiver(StreamWriter writer, int indentLevel = 0) {
            this.writer = writer;
            this.indentLevel = indentLevel;
        }

        public void Add(string line) {
            for (int i = 0; i < indentLevel; i++) writer.Write(INDENT);
            writer.WriteLine(line);
        }

        public void IndentIn() => indentLevel++;
        public void IndentOut() => indentLevel--;
    }
}