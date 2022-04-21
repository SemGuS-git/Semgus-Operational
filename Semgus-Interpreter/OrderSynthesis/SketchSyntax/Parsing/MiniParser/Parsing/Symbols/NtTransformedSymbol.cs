using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    internal class NtTransformedSymbol : TransformedSymbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<INode> items = new();

            private readonly NtTransformedSymbol source;
            private bool done = false;

            public Frame(NtTransformedSymbol source) {
                this.source = source;
            }

            public override void NotifyFailure() { }

            public override void NotifySuccess(IEnumerable<INode> ok) {
                items.AddRange(ok);
                IsSuccess = true;
            }

            public override IEnumerable<INode> Bake() => Just(source.Function(items));

            public override bool MoveNext() => !done && (done = true);
        }


        private readonly INonTerminalSymbol _inner;

        public NtTransformedSymbol(INonTerminalSymbol inner, Transformer function) : base((Symbol)inner,function){
            _inner = inner;
        }

        public override bool CheckTerminal(IToken token, out INode node) => throw new NotSupportedException();

        public ISynaxMatchingFrame GetFrame() => new Frame(this);
    }
}
