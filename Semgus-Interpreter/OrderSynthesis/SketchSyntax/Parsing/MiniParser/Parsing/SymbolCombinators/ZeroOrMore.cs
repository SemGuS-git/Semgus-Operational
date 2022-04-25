using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class ZeroOrMore : Symbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<ISyntaxNode> items = new();

            private readonly ZeroOrMore source;
            private bool done = false;

            public Frame(ZeroOrMore source) {
                this.source = source;
                IsSuccess = true;
            }

            public override void NotifyFailure() => done = true;

            public override void NotifySuccess(IEnumerable<ISyntaxNode> ok) => items.AddRange(ok);

            public override IEnumerable<ISyntaxNode> Bake() => items;// source.Transform is null ? items : new[] { source.Transform(items) };

            public override bool MoveNext() => !done;
        }

        public readonly Symbol Inner;

        public ZeroOrMore(Symbol a) {
            this.Inner = a;
        }
        public ISynaxMatchingFrame GetFrame() => new Frame(this);

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            Queue<ISyntaxNode> okResults = new();

            while (Inner.ParseRecursive(tokens) is ParseOk ok) {
                okResults.AddRange(ok.Value);
            }

            return new ParseOk(okResults);
        }
        public override string ToString() => $"{Inner.Name??Inner}*";
    }
}
