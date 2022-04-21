﻿using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class ZeroOrMore : Symbol, INonTerminalSymbol {
        private class Frame : FrameBase {
            public override Symbol Current => source.Inner;

            private Queue<INode> items = new();

            private readonly ZeroOrMore source;
            private bool done = false;

            public Frame(ZeroOrMore source) {
                this.source = source;
                IsSuccess = true;
            }

            public override void NotifyFailure() => done = true;

            public override void NotifySuccess(IEnumerable<INode> ok) => items.AddRange(ok);

            public override IEnumerable<INode> Bake() => items;// source.Transform is null ? items : new[] { source.Transform(items) };

            public override bool MoveNext() => !done;
        }

        public readonly Symbol Inner;

        public ZeroOrMore(Symbol a) {
            this.Inner = a;
        }
        public ISynaxMatchingFrame GetFrame() => new Frame(this);

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            Queue<INode> okResults = new();

            while (Inner.ParseRecursive(tokens) is ParseOk ok) {
                okResults.AddRange(ok.Value);
            }

            return new ParseOk(okResults);
        }
        public override string ToString() => $"{Inner.Name??Inner}*";
    }
}
