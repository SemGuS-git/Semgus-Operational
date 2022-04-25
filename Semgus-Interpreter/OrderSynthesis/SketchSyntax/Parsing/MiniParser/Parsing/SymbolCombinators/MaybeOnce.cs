using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    internal class MaybeOnce : Symbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<ISyntaxNode> items = new();


            private readonly MaybeOnce source;
            private bool done = false;

            public Frame(MaybeOnce source) {
                this.source = source;
                IsSuccess = true;
            }

            public override void NotifyFailure() { }

            public override void NotifySuccess(IEnumerable<ISyntaxNode> ok) {
                items.AddRange(ok);
            }

            public override IEnumerable<ISyntaxNode> Bake() => items;

            public override bool MoveNext() => !done && (done = true);
        }

        public readonly Symbol Inner;

        public MaybeOnce(Symbol a) {
            this.Inner = a;
        }

        public ISynaxMatchingFrame GetFrame() => new Frame(this);


        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            return new ParseOk(Inner
                .ParseRecursive(tokens)
                .OrDefault(Enumerable.Empty<ISyntaxNode>())!);
        }
        public override string ToString() => $"{Inner.Name ?? Inner}?";
    }
}
