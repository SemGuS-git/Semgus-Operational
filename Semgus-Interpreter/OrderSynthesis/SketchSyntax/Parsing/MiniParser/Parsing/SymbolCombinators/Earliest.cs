using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    using ParseResult = Result<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseOk = OkResult<IEnumerable<ISyntaxNode>, ParseError>;
    using ParseErr = ErrResult<IEnumerable<ISyntaxNode>, ParseError>;

    using ParseManyResult = Result<IEnumerable<ISyntaxNode>, IEnumerable<ParseError>>;
    using ParseManyOk = OkResult<IEnumerable<ISyntaxNode>, IEnumerable<ParseError>>;
    using ParseManyErr = ErrResult<IEnumerable<ISyntaxNode>, IEnumerable<ParseError>>;

    internal class Earliest : Symbol, INonTerminalSymbol {

        private class Frame : FrameBase {
            private readonly Earliest source;
            private readonly IEnumerator<Symbol> enumerator;

            public override Symbol Current => enumerator.Current;

            private bool stop = false;

            private Queue<ISyntaxNode> results = new();

            public Frame(Earliest source) {
                this.source = source;
                this.enumerator = source.list.GetEnumerator();
            }

            public override IEnumerable<ISyntaxNode> Bake() => results;// source.Transform is null ? results : new[] { source.Transform(results) };

            public override bool MoveNext() => !stop && enumerator.MoveNext();

            public override void NotifyFailure() { }

            public override void NotifySuccess(IEnumerable<ISyntaxNode> ok) {
                IsSuccess = true;
                results.AddRange(ok);
                stop = true;
            }
        }

        public bool CanDissolve { get; set; } = true;

        public List<Symbol> list = new List<Symbol>();

        public Earliest(params Symbol[] a) {
            list.AddRange(a);
        }

        public Earliest(IEnumerable<Symbol> a) {
            list.AddRange(a);
        }

        public override string ToString() => $"( {string.Join(" | ", list.Select(a=>a.Name??a.ToString()))} )";

        public static Earliest operator |(Earliest a, Earliest b)
            => a.CanDissolve
                ? b.CanDissolve
                    ? (new(a.list.Concat(b.list))) 
                    : (new(a.list.Append(b)))
                : b.CanDissolve
                    ? (new(b.list.Prepend(a))) 
                    : (new(a, b));

        public static Earliest operator |(Earliest a, Symbol b) => a.CanDissolve ? new(a.list.Append(b)) : new(a, b);
        public static Earliest operator |(Symbol a, Earliest b) => b.CanDissolve ? new(b.list.Prepend(a)) : new(a, b);

        public static Symbol Of(params Symbol[] s) => s.Length == 1 ? s[0] : new Earliest(s);
        //public static Earliest operator |(Earliest a, Earliest b) => new(a, b);
        //public static Earliest operator |(Earliest a, Symbol b) => new(a, b);
        //public static Earliest operator |(Symbol a, Earliest b) => new(a, b);
        //public static Symbol Of(params Symbol[] s) => new Earliest(s);

        public ISynaxMatchingFrame GetFrame() => new Frame(this);

        internal override ParseResult ParseRecursive(TapeEnumerator<IToken> tokens) {
            var c = tokens.Cursor;
            switch(list.Select(s => s.ParseRecursive(tokens)).FirstOne()) {
                case ParseManyOk ok:
                    return new ParseOk(ok.Value);
                case ParseManyErr err:
                    tokens.Cursor = c;
                    return new ParseErr(new(tokens, this, err.Error));
                default:  throw new NotSupportedException();
            };
        }
    }
}
