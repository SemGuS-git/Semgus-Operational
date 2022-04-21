using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class KeywordSymbol : Symbol, INode {
        public string Value;
        private readonly bool noemit;

        public KeywordSymbol(string s, bool noemit = false) {
            this.Value = s;
            this.noemit = noemit;
        }

        public override bool CheckTerminal(IToken token, out INode node) {
            if (token.Is(Value)) {
                node = noemit ? Empty.Instance : this;
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
                return new ParseOk(noemit ? Enumerable.Empty<INode>() : new[] { this });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
