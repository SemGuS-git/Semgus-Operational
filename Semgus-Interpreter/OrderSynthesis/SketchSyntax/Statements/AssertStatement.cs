namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record AssertStatement  (IExpression Expr)  : IStatement  {
        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"assert({Expr});");
        }
        public override string ToString() => this.PrettyPrint(true);

    }
}
