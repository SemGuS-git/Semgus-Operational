namespace Semgus.MiniParser {
    internal struct LineComment : IToken {
        public readonly string Value;

        public LineComment(string value) {
            this.Value = value;
        }

        public override string ToString() => "//" + Value;
    }
}
