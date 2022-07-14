
using Semgus.Util;
using System.Collections;

namespace Semgus.MiniParser {
    internal class TokenSet {
        private readonly HashSet<string> keywordSet;
        private readonly CharTrie<IToken> specialTokenTrie;

        public TokenSet(IEnumerable<string> keywords, IEnumerable<string> special, IReadOnlyDictionary<string, string> substitutions) {
            keywordSet = keywords.ToHashSet();

            specialTokenTrie = CharTrie<IToken>.Build(special.Select(SpecialToken.Of));
            foreach (var kvp in substitutions) specialTokenTrie.Insert(kvp.Key, SpecialToken.Of(kvp.Value).Item2);
        }

        public static TokenSet ForSketch { get; } = new(
            "if else return assert assume minimize new repeat ref harness generator struct implements".Split(' '),
                "=+-*<>!(){},;?:.".Select(c => new string(c, 1)).Concat("== != || && <= >= ?? --".Split(' ')),
                new Dictionary<string, string>() { { "//{};", ";" } } // handle weird junk line endings
            );


        class ScanEnumerable : IEnumerable<IToken> {
            private TokenSet tokenSet;
            private string text;

            public ScanEnumerable(TokenSet tokenSet, string text) {
                this.tokenSet = tokenSet;
                this.text = text;
            }

            public IEnumerator<IToken> GetEnumerator() => new Lexer(tokenSet.keywordSet, tokenSet.specialTokenTrie, text);


            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public IEnumerable<IToken> Scan(string text) => new ScanEnumerable(this, text);
    }
}
