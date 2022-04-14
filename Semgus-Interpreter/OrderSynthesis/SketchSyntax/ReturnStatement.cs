namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class ReturnStatement : IStatement {
        public IExpression expr { get; }

        public ReturnStatement(IExpression expr) {
            this.expr = expr;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"return {expr};");
        }
    }
}
