using Semgus.Util;
using System.Text;

namespace Semgus.MiniParser {
    internal class ParseError : Exception {
        public ITape<IToken> Tape { get; }
        public Symbol Symbol { get; private set; }
        public IReadOnlyList<ParseError> ChildErrors { get; }
        public int TokenIndexStart { get; private set; }
        public int TokenCount { get; private set; }

        public int Height { get; private set; }

        private string _msg;
        public override string Message => _msg ??= PrettyPrint();


        public string PrettyPrint() {
            var sb = new StringBuilder();
            sb.AppendLine("Parse error");
            PrettyPrint(sb, 0);
            sb.Append("Nearby tokens: [ ");
            for(int i = Math.Max(0,TokenIndexStart-5); i < TokenIndexStart; i++) {
                sb.Append(Tape[i].Unwrap());
                sb.Append(' ');
            }
            sb.Append("<!> ");
            for (int i = 0; i < TokenCount; i++) {
                if (!Tape[TokenIndexStart + i].TryGetValue(out var t)) break;
                sb.Append(t);
                sb.Append(' ');
            }
            sb.Append("<!> ");

            for (int i = 0; i < 5; i++) {
                if (!Tape[TokenIndexStart+TokenCount+ i].TryGetValue(out var t)) break;
                sb.Append(t);
                sb.Append(' ');
            }
            sb.AppendLine("]");
            sb.AppendLine("--- end ---");
            return sb.ToString();
        }
        public void PrettyPrint(StringBuilder sb, int depth) {
            sb.Append($"at [{TokenIndexStart}:{TokenIndexStart + TokenCount}: {Tape[TokenIndexStart].Unwrap()} ]");
            sb.Append(new string(' ', 4*depth++));
            if (Symbol.Name is null) {
                sb.Append("(...)");
            } else {
                sb.Append(Symbol.Name);
            }
            sb.Append(" ::= ");
            sb.AppendLine(Symbol.ToString());
            foreach (var ch in ChildErrors) ch.PrettyPrint(sb, depth);
        }

        internal ParseError(TapeEnumerator<IToken> tokens, Symbol symbol, int tokenIndexStart, int tokenCount = 1, int height = 1) {
            this.Tape = tokens.Tape;
            this.Symbol = symbol;
            this.ChildErrors = Array.Empty<ParseError>();
            this.TokenIndexStart = tokenIndexStart;
            this.TokenCount = tokenCount;
            Height = height;
        }

        public ParseError(TapeEnumerator<IToken> tokens, Symbol nt, IEnumerable<ParseError> childErrors) {
            this.Tape = tokens.Tape;
            this.Symbol = nt;
            this.ChildErrors = childErrors.ToList();
            this.TokenIndexStart = ChildErrors[0].TokenIndexStart;
            this.TokenCount = ChildErrors.Max(ch => ch.TokenIndexStart + ch.TokenCount) - TokenIndexStart;
            Height = ChildErrors.Max(ch => ch.Height) + 1;
        }
        public ParseError(TapeEnumerator<IToken> tokens, Symbol nt, ParseError childError) {
            this.Tape = tokens.Tape;
            this.Symbol = nt;
            this.ChildErrors = new[] { childError };
            this.TokenIndexStart = childError.TokenIndexStart;
            this.TokenCount = childError.TokenCount;
            Height = childError.Height + 1;
        }
    }
}
