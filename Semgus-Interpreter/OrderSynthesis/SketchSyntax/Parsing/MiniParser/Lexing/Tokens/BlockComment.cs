namespace Semgus.MiniParser {
    internal struct BlockComment : IToken {
        public readonly string Value;

        public BlockComment(string value) {
            this.Value = value;
        }
        public override string ToString() => "/*" + Value + "*/";

    }
}
