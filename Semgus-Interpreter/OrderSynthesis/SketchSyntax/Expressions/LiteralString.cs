namespace Semgus.OrderSynthesis.SketchSyntax {
    internal struct LiteralString : IExpression {
        public string Value { get; }

        public LiteralString(string value) {
            Value = value;
        }

        public override string ToString() => $"\"{Value}\"";
    }
}
