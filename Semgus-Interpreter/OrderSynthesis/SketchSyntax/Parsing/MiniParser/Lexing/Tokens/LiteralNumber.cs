namespace Semgus.MiniParser {
    internal struct LiteralNumber : IToken {
        public readonly int Value;

        public LiteralNumber(int value) {
            Value = value;
        }

        public override string ToString() => Value.ToString();
        public static LiteralNumber Zero { get; } = new(0);
    }
}
