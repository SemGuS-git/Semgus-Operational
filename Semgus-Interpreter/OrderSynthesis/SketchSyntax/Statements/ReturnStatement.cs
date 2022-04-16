namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record ReturnStatement(IExpression? Expr = null) : IStatement {
        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add(Expr is null ? "return;" : $"return {Expr};");
        }
        public override string ToString() => this.PrettyPrint(true);
    }
}
