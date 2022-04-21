using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class MaybeOnce : Symbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<INode> items = new();


            private readonly MaybeOnce source;
            private bool done = false;

            public Frame(MaybeOnce source) {
                this.source = source;
                IsSuccess = true;
            }

            public override void NotifyFailure() { }

            public override void NotifySuccess(IEnumerable<INode> ok) {
                items.AddRange(ok);
            }

            public override IEnumerable<INode> Bake() => items;

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
                .OrDefault(Enumerable.Empty<INode>())!);
        }
        public override string ToString() => $"{Inner.Name ?? Inner}?";
    }
}
