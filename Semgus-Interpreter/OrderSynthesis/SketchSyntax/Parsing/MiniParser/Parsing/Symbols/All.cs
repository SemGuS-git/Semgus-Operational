using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<INode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<INode>, ParseError>;

    using ParseManyResult = Result<IEnumerable<INode>, ParseError>;
    using ParseManyOk = OkResult<IEnumerable<INode>, ParseError>;
    using ParseManyErr = ErrResult<IEnumerable<INode>, ParseError>;

    internal class All : Symbol, INonTerminalSymbol {

        private class Frame : FrameBase {
            private readonly All source;
            private readonly IEnumerator<Symbol> enumerator;

            public override Symbol Current => enumerator.Current;

            private bool stop = false;
            private bool anyFailed = false;

            private Queue<INode> results = new();

            public Frame(All source) {
                this.source = source;
                this.enumerator = source.list.GetEnumerator();
            }

            public override IEnumerable<INode> Bake() => results;//: new[] { source.Transform(results) };

            public override bool MoveNext() {
                if (stop) return false;
                if (enumerator.MoveNext()) return true;
                stop = true;
                IsSuccess = true;
                return false;
            }

            public override void NotifyFailure() {
                stop = true;
                IsSuccess = false;
            }

            public override void NotifySuccess(IEnumerable<INode> ok) {
                results.AddRange(ok);
            }
        }

        public List<Symbol> list = new List<Symbol>();

        public All(params Symbol[] a) {
            list.AddRange(a);
        }

        public All(IEnumerable<Symbol> a) {
            list.AddRange(a);
        }


        public override string ToString() => $"( {string.Join(" ", list.Select(a => a.Name ?? a.ToString()))} )";



        public static All operator +(All a, All b) => new(a.list.Concat(b.list));
        public static All operator +(All a, Symbol b) => new(a.list.Append(b));
        public static All operator +(Symbol a, All b) => new(b.list.Prepend(a));
        public static Symbol Of(params Symbol[] s) => s.Length == 1 ? s[0] : new All(s);

        //public static All operator +(All a, All b) => new(a, b);
        //public static All operator +(All a, Symbol b) => new(a, b);
        //public static All operator +(Symbol a, All b) => new(a, b);
        //public static Symbol Of(params Symbol[] s) => new All(s);


        public ISynaxMatchingFrame GetFrame() => new Frame(this);


        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            var c = tokens.Cursor;
            switch (list.Select(s => s.ParseRecursive(tokens)).Collect().Select(r => r.SelectMany(a => a))) {
                case ParseManyOk ok:
                    return new ParseOk(ok.Value);
                case ParseManyErr err:
                    tokens.Cursor = c;
                    return new ParseErr(new(tokens, this, err.Error));
                default: throw new NotSupportedException();
            };
        }
    }
}
