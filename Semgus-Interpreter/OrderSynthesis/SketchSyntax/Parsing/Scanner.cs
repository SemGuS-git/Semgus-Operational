using System.Collections;

namespace Semgus.OrderSynthesis.SketchSyntax.Parsing.Iota {
    internal class Scanner : IEnumerator<ILexeme> {
        private ILexeme? current = null;
        public ILexeme Current => current ?? throw new InvalidOperationException();

        object IEnumerator.Current => Current;

        private int cursor = 0;

        private readonly string src;
        private readonly int n;
        private readonly int n_minus_one;

        private static readonly Trie<ILexeme> SpecialTokenTrie = Trie<ILexeme>.Build(
            "=+-*<>!(){},;".Select(SpecialChar.Of),
            "== != || && <= >=".Split(' ').Select(SpecialMore.Of)
        );

        public Scanner(string src) {
            this.src = src;
            n = src.Length;
            n_minus_one = n - 1;
        }

        public void Reset() => new NotSupportedException();

        public void Dispose() { }

        public bool MoveNext() {

            ILexeme token = default(None);
            if (ReadNextToken(ref token)) {
                current = token;
                return true;
            }

            if (cursor > n_minus_one) return false;

            throw new InvalidTokenException(ReadToNextWhitespace());
        }

        string ReadToNextWhitespace() {
            int start = cursor;
            while (cursor < n && !char.IsWhiteSpace(src[cursor])) {
                cursor++;
            }
            return src[start..cursor];
        }

        bool ReadNextToken(ref ILexeme token) {
            while (cursor < n && char.IsWhiteSpace(src[cursor])) {
                cursor++;
            }
            return cursor < n && ReadToken(ref token);
        }

        bool ReadToken(ref ILexeme token)
            => ReadComment(ref token)
            || ReadTrieMember(ref token)
            || ReadNumber(ref token)
            || ReadWord(ref token);


        bool ReadTrieMember(ref ILexeme token) {
            if (!SpecialTokenTrie.TryGet(src[cursor], out var trie)) return false;

            ILexeme? longest_ok = null;

            int t = cursor;
            while (++t < n && trie.TryGet(src[t], out trie)) {
                if (trie.Value is not null) {
                    cursor = t;
                    longest_ok = trie.Value;
                }
            }

            if (longest_ok is null) {
                return false;
            } else {
                token = longest_ok;
                return true;
            }
        }



        private bool ReadComment(ref ILexeme token) {
            if (src[cursor] != '/' || cursor == n_minus_one) return false;

            switch (src[cursor + 1]) {
                case '/':
                    TakeLineComment(out token);
                    return true;
                case '*':
                    TakeBlockComment(out token);
                    return true;
                default:
                    return false;
            }
        }

        private void TakeLineComment(out ILexeme token) {
            int end = cursor + 2;

            while (++end < n && src[end] != '\n') { }

            token = new LineComment(src.Substring(cursor + 2, end - cursor - 1));
            cursor = end;
        }

        private void TakeBlockComment(out ILexeme token) {
            int end = cursor + 3;
            while (++end < n) {
                if (src[end - 1] == '*' && src[end] == '/') {
                    token = new BlockComment(src.Substring(cursor + 2, cursor - end - 1));
                    cursor = end;
                    return;
                }
            }

            throw new InvalidTokenException(src[cursor..n], "Unexpected end of input while parsing block comment");
        }


        private static bool IsIdentifierChar(char next) => (char.IsLetterOrDigit(next) || next == '@' || next == '_');

        private bool ReadNumber(ref ILexeme token) {
            if (src[cursor] == '0') {
                if (cursor < n_minus_one && IsIdentifierChar(src[cursor + 1])) return false;

                token = Literal.Zero;
                cursor++;

                return true;
            }

            int t = cursor;

            while (char.IsDigit(src[t]) && ++t < n) { }
            if (t == cursor || (t < n && IsIdentifierChar(src[t]))) return false;

            token = new Literal(int.Parse(src.AsSpan().Slice(cursor, cursor - t)));
            cursor = t;
            return true;
        }

        private bool ReadWord(ref ILexeme token) {
            char head = src[cursor];

            if (!(char.IsLetter(head) || head == '_' || head == '@')) return false;

            int t = cursor + 1;

            while (t < n && IsIdentifierChar(src[t++])) { }

            token = new Word(src[cursor..t]);
            cursor = t;
            return true;
        }
    }
}
