namespace Semgus.MiniParser {
    internal struct SpecialToken : IToken {

        public readonly string Value;

        public SpecialToken(string value) {
            Value = value;
        }
        public static (string, IToken) Of(string value) => (value, new SpecialToken(value));

        public bool Is(string s) => Value == s;
        public bool Is(char s) => Value.Length == 1 && Value[0] == s;
        public bool IsAnyOf(HashSet<string> s, out string? which) {
            if (s.Contains(Value)) {
                which = Value;
                return true;
            } else {
                which = null;
                return false;
            }
        }

        public override string ToString() => Value;
    }
}
