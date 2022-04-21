using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class LiteralSymbol : Symbol {
        public override string ToString() => Name ?? "INT";

        public override bool CheckTerminal(IToken token, out INode node) {
            if (token is LiteralNumber lit) {
                node = new Literal(lit.Value);
                return true;
            } else {
                node = default;
                return false;
            }
        }

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            if (tokens.Peek().TryGetValue(out var token) && token is LiteralNumber lit) {
                tokens.MoveNext();
                return new ParseOk(new INode[] { new Literal(lit.Value) });
            } else {
                return new ParseErr(new(tokens, this, tokens.Cursor, 1));
            }
        }
    }
}
