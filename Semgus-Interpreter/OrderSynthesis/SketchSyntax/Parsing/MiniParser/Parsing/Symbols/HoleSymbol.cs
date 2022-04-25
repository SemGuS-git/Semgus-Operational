using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class HoleSymbol : Symbol {
        public override string ToString() => Name ?? "ID";

        public override bool CheckTerminal(IToken token, out ISyntaxNode node) {
            if (token.Is("??")) {
                node = new Hole();
                return true;
            } else {
                node = default;
                return false;
            }
        }

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            if (tokens.Peek().TryGetValue(out var token) && token.Is("??")) {
                tokens.MoveNext();
                return new ParseOk(new[] { new Hole() });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
