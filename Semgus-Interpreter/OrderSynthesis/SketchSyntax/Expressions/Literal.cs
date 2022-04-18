namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Literal  (int Value)  : IExpression {

        public Literal(object boxedValue) : this(boxedValue switch {
            bool b => b ? 1 : 0,
            int i => i,
            long l => Convert.ToInt32(l),
            _ => throw new NotSupportedException(),
        }) { }
        public override string ToString() => Value.ToString();
    }
}
