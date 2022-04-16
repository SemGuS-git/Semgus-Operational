namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record MinimizeStatement  (IExpression expr) : IStatement  {
        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"minimize({expr});");
        }
        public override string ToString() => this.PrettyPrint(true);
    }
}
