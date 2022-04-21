namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Annotation(string Comment, int Gap = 0) : IStatement {
        public void WriteInto(ILineReceiver lineReceiver) {
            for (int i = 0; i < Gap; i++) lineReceiver.Add("");
            lineReceiver.Add("// " + Comment);
        }
    }
}
