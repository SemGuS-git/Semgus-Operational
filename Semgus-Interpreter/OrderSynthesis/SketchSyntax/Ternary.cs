namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class Ternary : IExpression {
        public IExpression Cond { get; }
        public IExpression ValIf { get; }
        public IExpression ValElse { get; }

        public Ternary(IExpression cond, IExpression valIf, IExpression valElse) {
            Cond = cond;
            ValIf = valIf;
            ValElse = valElse;
        }

        public override string ToString() => $"({Cond} ? {ValIf} : {ValElse})";
    }
}
