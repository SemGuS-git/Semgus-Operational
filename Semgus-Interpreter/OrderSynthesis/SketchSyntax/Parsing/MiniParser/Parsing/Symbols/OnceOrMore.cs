using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class OnceOrMore : Symbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<INode> items = new();


            private readonly OnceOrMore source;
            private bool done = false;

            public Frame(OnceOrMore source) {
                this.source = source;
            }


            public override void NotifyFailure() => done = true;

            public override void NotifySuccess(IEnumerable<INode> ok) {
                items.AddRange(ok);
                IsSuccess = true;
            }

            public override IEnumerable<INode> Bake() => items;// source.Transform is null ? items : new[] { source.Transform(items) };

            public override bool MoveNext() => !done;
        }

        public readonly Symbol Inner;

        public OnceOrMore(Symbol symbol) {
            this.Inner = symbol;
        }

        public ISynaxMatchingFrame GetFrame() => new Frame(this);
        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            int c = tokens.Cursor;
            bool any = false;
            Queue<INode> okResults = new();

            ParseResult next;

            while ((next = Inner.ParseRecursive(tokens)) is ParseOk ok) {
                any = true;
                okResults.AddRange(ok.Value);
            }

            if (any) {
                return new ParseOk(okResults);
            } else {
                tokens.Cursor = c;
                return new ParseErr(new(tokens, this, ((ParseErr)next).Error));
            }
        }
        public override string ToString() =>  $"{Inner.Name ?? Inner}+";
    }
}
