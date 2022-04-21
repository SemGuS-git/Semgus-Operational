using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    internal class TransformedSymbol : Symbol {
        public override string? Name {
            get { return Inner.Name; }
            set { Inner.Name = value; }
        }

        public Symbol Inner { get; }

        public Transformer Function { get; }

        public delegate INode Transformer(Queue<INode> context);

        public TransformedSymbol(Symbol inner, Transformer function) {
            Inner = inner;
            Function = function;
        }

        public override bool CheckTerminal(IToken token, out INode node) {
            if (!Inner.CheckTerminal(token, out node)) return false;
            node = Function(new(Just(node)));
            return true;
        }

        public override string ToString() => "TR";// Inner.ToString();

        internal override Result<IEnumerable<INode>, ParseError> ParseRecursive(TapeEnumerator<IToken> tokens) {
            var innerResult = Inner.ParseRecursive(tokens);
            if (innerResult is ErrResult<IEnumerable<INode>, ParseError> err) return innerResult;
            var nodes = innerResult.Unwrap();
            try {
                var transformed = Function(new(nodes));
                return Result.Ok<IEnumerable<INode>, ParseError>(Just(transformed));
            } catch (Exception e) {
                throw new AggregateException($"Error constructing symbol {Name??"(...)"} ::= {Inner} from [{string.Join(", ",nodes)}]", e);
            }
        }

        protected static IEnumerable<T> Just<T>(T value) {
            yield return value;
        }
    }
}
