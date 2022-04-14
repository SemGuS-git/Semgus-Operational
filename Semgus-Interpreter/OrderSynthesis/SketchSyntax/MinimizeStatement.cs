namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class MinimizeStatement: IStatement {
        public IExpression expr { get; }

        public MinimizeStatement(IExpression expr) {
            this.expr = expr;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"minimize({expr});");
        }
    }
}
