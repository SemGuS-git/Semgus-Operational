namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class LineComment : IStatement {
        public string Comment { get; }
        public int Gap { get; }

        public LineComment(string comment, int gap = 0) {
            Comment = comment;
            Gap = gap;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            for (int i = 0; i < Gap; i++) lineReceiver.Add("");
            lineReceiver.Add("// " + Comment);
        }
    }
}
