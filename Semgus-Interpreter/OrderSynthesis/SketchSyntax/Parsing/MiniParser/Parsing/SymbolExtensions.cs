using Semgus.OrderSynthesis;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {

    internal static class SymbolExtensions {
        public static ZeroOrMore Star(this Symbol a) => new(a);
        public static OnceOrMore Some(this Symbol a) => new(a);
        public static MaybeOnce Maybe(this Symbol a) => new(a);

        private static Dictionary<string, UnaryOp> UnaryMap { get; } = Enum.GetValues<UnaryOp>().ToDictionary(r => r.Str());
        private static Dictionary<string, Op> InfixMap { get; } = Enum.GetValues<Op>().ToDictionary(r => r.Str());


        public static TransformedSymbol Transform(this Symbol s, TransformedSymbol.Transformer t) {
            if (s is INonTerminalSymbol nt) return new NtTransformedSymbol(nt,t);
            return new TransformedSymbol(s,t);
        }

        public static Symbol Prefix(this Symbol term, params Symbol[] ops) => (Earliest.Of(ops) + term)
            .Transform(ctx => new UnaryOperation(ctx.TakeKeywordFrom(UnaryMap), ctx.Take<IExpression>()));

        public static Symbol Binary(this Symbol term, params Symbol[] ops) => (term + Maybe(Earliest.Of(ops) + term))
            .Transform((ctx) => {
                var head = ctx.Take<IExpression>();

                if (ctx.TryTakeKeywordFrom(InfixMap, out var op)) {
                    var tail = ctx.Take<IExpression>();
                    return new InfixOperation(op, head, tail);
                } else {
                    return head;
                }
            });

        public static Symbol Infix(this Symbol term, params Symbol[] ops) => (term + Star(Earliest.Of(ops) + term))
            .Transform((ctx) => {
                var head = ctx.Take<IExpression>();

                List<(Op, IExpression)> tail = new();
                while (ctx.TryTakeKeywordFrom(InfixMap, out var op)) {
                    tail.Add((op, ctx.Take<IExpression>()));
                }

                if (tail.Count == 0) return head;
                return InfixOperation.GroupOperators(head, tail);
            }
        );

        public static Result<IEnumerable<INode>, ParseError> ParseString(this Symbol symbol, string raw) {
            var tokens = TokenSet.ForSketch.Scan(raw).Where(t => t is not LineComment && t is not BlockComment);
            var tape = new TapeEnumerator<IToken>(new AccumulatorTape<IToken>(tokens));
            tape.MoveNext();
            return symbol.ParseRecursive(tape);
        }

        //public static bool TryParse(this Symbol symbol, string raw, out IEnumerable<INode> result) => new SyntaxParser(TokenSet.ForSketch.Scan(raw)).TryParse(symbol, out result);
    }
}
