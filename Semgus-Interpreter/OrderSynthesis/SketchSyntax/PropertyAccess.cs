namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class PropertyAccess : IExpression, ISettable {
        public IExpression Expr { get; }
        public VarId Prop { get; }

        public PropertyAccess(IExpression expr, VarId prop) {
            this.Expr = expr;
            this.Prop = prop;
        }

        public override string ToString() => $"{Expr}.{Prop}";
    }
}
