namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Ternary(IExpression Cond, IExpression ValIf, IExpression ValElse) : IExpression {
        public override string ToString() => $"{(Cond is Ternary ? $"({Cond})" : Cond)} ? {(ValIf is Ternary ? $"({ValIf})" : ValIf)} : {ValElse}";
    }
}
