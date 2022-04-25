using Semgus.Util;

namespace Semgus.MiniParser {
    internal class Parser<T> {
        public readonly Symbol Symbol;

        public Parser(Symbol s) {
            this.Symbol = s;
        }
        public Result<T, ParseError> TryParse(string input) => Symbol.ParseString(input).Select(seq => seq.Cast<T>().Single());

        public IEnumerable<T> ParseMany(string input) {
            var a = Symbol.ParseString(input).Unwrap();
            return a.Cast<T>();
        }

        public T Parse(string input) => ParseMany(input).Single();

    }
}
