namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record ReturnStatement(IExpression Expr) : IStatement {
        public ReturnStatement() : this(Empty.Instance) { }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add(Expr is Empty ? "return;" : $"return {Expr};");
        }
        public override string ToString() => this.PrettyPrint(true);
    }
}
