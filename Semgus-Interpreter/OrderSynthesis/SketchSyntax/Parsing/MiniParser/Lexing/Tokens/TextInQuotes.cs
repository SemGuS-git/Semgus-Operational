namespace Semgus.MiniParser {
    internal struct TextInQuotes : IToken {
        public readonly string Value;

        public TextInQuotes(string value) {
            Value = value;
        }

        public override string ToString() => $"\"{Value}\"";
    }
}
