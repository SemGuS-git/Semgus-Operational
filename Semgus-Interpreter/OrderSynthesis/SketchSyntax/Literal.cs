namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class Literal : IExpression {
        public int Value { get; }

        public Literal(int value) {
            Value = value;
        }

        public Literal(object boxedValue) {
            Value = boxedValue switch {
                bool b => b ? 1 : 0,
                int i => i,
                long l => Convert.ToInt32(l),
                _ => throw new NotSupportedException(),
            };
        }

        public override string ToString() => Value.ToString();
    }
}
