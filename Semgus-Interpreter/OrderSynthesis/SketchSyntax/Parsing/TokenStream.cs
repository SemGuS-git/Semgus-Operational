using System.Collections;

namespace Semgus.OrderSynthesis.SketchSyntax.Parsing.Iota {
    internal class TokenStream : IEnumerable<ILexeme> {
        private readonly string src;
        public TokenStream(string src) {
            this.src = src;
        }

        public IEnumerator<ILexeme> GetEnumerator() => new Scanner(src);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
