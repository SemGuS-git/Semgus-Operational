using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal struct Literal : IExpression {
        public int Value { get; }
        
        public Literal(int value) {
            Value = value;
        }

        public Literal(object boxedValue) : this(boxedValue switch {
            bool b => b ? 1 : 0,
            int i => i,
            long l => Convert.ToInt32(l),
            _ => throw new NotSupportedException(),
        }) { }
        public override string ToString() => Value.ToString();
    }
}
