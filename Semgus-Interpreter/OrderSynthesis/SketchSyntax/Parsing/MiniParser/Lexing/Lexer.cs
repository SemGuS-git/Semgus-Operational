
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;
using System.Collections;

namespace Semgus.MiniParser {
    internal class Lexer : IEnumerator<IToken> {
        private IToken? current = null;
        public IToken Current => current ?? throw new InvalidOperationException();

        object IEnumerator.Current => Current;

        private int cursor = 0;
        private readonly string src;
        private readonly int n;
        private readonly int n_minus_one;


        private readonly HashSet<string> keywords;
        private readonly CharTrie<IToken> trieRoot;
        public Lexer(HashSet<string> keywords, CharTrie<IToken> trie, string src) {
            this.keywords = keywords;
            this.trieRoot = trie;
            this.src = src;
            n = src.Length;
            n_minus_one = n - 1;
        }

        public void Reset() => new NotSupportedException();

        public void Dispose() { }

        public bool MoveNext() {

            IToken token = default(None);
            if (ReadNextToken(ref token)) {
                current = token;
                return true;
            }

            if (cursor > n_minus_one) return false;

            int c0 = cursor;
            ReadToNextWhitespace();
            int c1 = cursor;

            throw new InvalidTokenException(src, c0, c1);
        }

        string ReadToNextWhitespace() {
            int start = cursor;
            while (cursor < n && !char.IsWhiteSpace(src[cursor])) {
                cursor++;
            }
            return src[start..cursor];
        }

        bool ReadNextToken(ref IToken token) {
            while (cursor < n && char.IsWhiteSpace(src[cursor])) {
                cursor++;
            }
            return cursor < n && ReadToken(ref token);
        }

        bool ReadToken(ref IToken token)
            => ReadTrieMember (ref token)
            || ReadComment(ref token)
            || ReadNumber(ref token)
            || ReadWord(ref token);


        bool ReadTrieMember(ref IToken token) {
            if (!trieRoot.TryGet(src[cursor], out var trie)) return false;

            IToken? longest_ok = trie.Value;

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
                cursor = t;
                return true;
            }
        }



        private bool ReadComment(ref IToken token) {
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

        private void TakeLineComment(out IToken token) {
            int start = cursor + 2;
            int end = start;

            while (end < n && src[end++] != '\n') { }


            token = new LineComment(src.Substring(start, end - start - 1));
            cursor = end;
        }

        private void TakeBlockComment(out IToken token) {
            int start = cursor + 2;
            int end = start + 1;
            while (++end < n) {
                if (src[end - 1] == '*' && src[end] == '/') {
                    token = new BlockComment(src.Substring(start, end - cursor - 1));
                    cursor = end + 1;
                    return;
                }
            }


            throw new InvalidTokenException(src, cursor, n, "Unexpected end of input while parsing block comment");
        }


        private static bool IsIdentifierChar(char next) => (char.IsLetterOrDigit(next) || next == '@' || next == '_');

        private bool ReadNumber(ref IToken token) {
            if (src[cursor] == '0') {
                if (cursor < n_minus_one && IsIdentifierChar(src[cursor + 1])) return false;

                token = LiteralNumber.Zero;
                cursor++;

                return true;
            }

            int t = cursor;

            while (t < n && char.IsDigit(src[t])) t++;

            if (t == cursor || (t < n && IsIdentifierChar(src[t]))) return false;

            if (!int.TryParse(src.AsSpan().Slice(cursor, t - cursor), out var value)) {
                throw new InvalidTokenException(src, cursor, t, "Failed to parse number");
            }

            token = new LiteralNumber(value);
            cursor = t;
            return true;
        }

        private bool ReadWord(ref IToken token) {
            char head = src[cursor];

            if (!(char.IsLetter(head) || head == '_' || head == '@')) return false;

            int t = cursor;

            while (++t < n && IsIdentifierChar(src[t])) { }

            var str = src[cursor..t];

            if (keywords.Contains(str)) {
                token = new Keyword(str);
            } else {
                token = new Identifier(str);
            }

            cursor = t;
            return true;
        }
    }
}
