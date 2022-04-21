namespace Semgus.MiniParser {
    internal struct Keyword : IToken {
        public readonly string Value;

        public Keyword(string value) {
            this.Value = value;
        }

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
