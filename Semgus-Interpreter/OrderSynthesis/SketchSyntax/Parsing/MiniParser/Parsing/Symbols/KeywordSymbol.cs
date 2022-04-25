using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class KeywordInstance : ISyntaxNode {
        public KeywordSymbol Keyword { get; }
        public string Value => Keyword.Value;

        public KeywordInstance(KeywordSymbol keyword) {
            Keyword = keyword;
        }

    }

    internal class KeywordSymbol : Symbol {
        public string Value { get; }
        private readonly bool noemit;
        private readonly KeywordInstance instance;

        public KeywordSymbol(string s, bool noemit = false) {
            this.Value = s;
            this.noemit = noemit;
            this.instance = new(this);
        }

        public override bool CheckTerminal(IToken token, out ISyntaxNode node) {
            if (token.Is(Value)) {
                node = noemit ? Empty.Instance : instance;
                return true;
            } else {
                node = Empty.Instance;
                return false;
            }
        }

        public override string ToString() => Name ?? $"\"{Value}\"";
        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            if (tokens.Peek().TryGetValue(out var token) && token.Is(Value)) {
                tokens.MoveNext();
                return new ParseOk(noemit ? Enumerable.Empty<ISyntaxNode>() : new[] { instance });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
