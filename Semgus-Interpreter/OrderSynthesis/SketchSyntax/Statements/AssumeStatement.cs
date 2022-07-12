namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record AssumeStatement(IExpression Expr) : IStatement {
        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"assume {Expr};");
        }
        public override string ToString() => this.PrettyPrint(true);

    }
}
