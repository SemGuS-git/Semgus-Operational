namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class AssertStatement : IStatement {
        public IExpression Expr { get; }
        public AssertStatement(IExpression expr) {
            this.Expr = expr;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"assert({Expr});");
        }
    }
}
