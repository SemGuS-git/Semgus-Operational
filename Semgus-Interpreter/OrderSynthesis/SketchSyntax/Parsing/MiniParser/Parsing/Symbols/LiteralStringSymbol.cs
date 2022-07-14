using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class LiteralStringSymbol : Symbol {
        public override string ToString() => Name ?? "STR";

        public override bool CheckTerminal(IToken token, out ISyntaxNode node) {
            if (token is TextInQuotes lit) {
                node = new LiteralString(lit.Value);
                return true;
            } else {
                node = default;
                return false;
            }
        }

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            if (tokens.Peek().TryGetValue(out var token) && token is TextInQuotes lit) {
                tokens.MoveNext();
                return new ParseOk(new ISyntaxNode[] { new LiteralString(lit.Value) });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
