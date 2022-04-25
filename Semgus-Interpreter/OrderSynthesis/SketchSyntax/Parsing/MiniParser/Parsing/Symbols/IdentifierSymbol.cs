using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class IdentifierSymbol : Symbol {
        public override string ToString() => Name ?? "ID";

        public override bool CheckTerminal(IToken token, out ISyntaxNode node) {
            if (token is Identifier id) {
                node = id;
                return true;
            } else {
                node = default;
                return false;
            }
        }
        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            if (tokens.Peek().TryGetValue(out var token) && token is Identifier id) {
                tokens.MoveNext();
                return new ParseOk(new ISyntaxNode[] { id });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
