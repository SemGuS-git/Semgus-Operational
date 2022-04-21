using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    internal abstract class Symbol {
        public virtual string? Name { get; set; }

        public static All operator +(Symbol a, Symbol b) => (a is All aa ? aa : new All(a)) + b;


        public static Earliest operator |(Symbol a, Symbol b) => (a is Earliest aa ? aa : new Earliest(a)) | b;

        public static implicit operator Symbol(string s) => s switch {
            "(" or ")" or "{" or "}" or ";" or "," => new KeywordSymbol(s,true),
            _ => new KeywordSymbol(s),
        };

        public virtual bool CheckTerminal(IToken token, out INode node) => throw new NotSupportedException();
        internal abstract Result<IEnumerable<INode>, ParseError> ParseRecursive(TapeEnumerator<IToken> tokens);
    }
}
