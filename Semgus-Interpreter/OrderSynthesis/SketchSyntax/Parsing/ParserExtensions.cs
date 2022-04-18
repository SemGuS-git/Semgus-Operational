
using Sprache;

namespace Semgus.OrderSynthesis.SketchSyntax.Parsing {
    internal static class ParserExtensions {
        static readonly CommentParser Comments = new();
        //public static Parser<T> Isol<T>(this Parser<T> parser) =>
        //    from leading in SketchParser.NonSemantic.Many()
        //    from item in parser
        //    from trailing in SketchParser.NonSemantic.Many()
        //    select item;

        public static Parser<T> Isol<T>(this Parser<T> parser) => parser.Token().Commented(Comments).Select(c => c.Value);
        public static Parser<T> WithSemicolon<T>(this Parser<T> parser) => parser.Then(a => Parse.Char(';').Isol().Return(a));
        public static Parser<T> WithSemicolonOrWeirdThing<T>(this Parser<T> parser) => parser.Then(a => Parse.Char(';').Isol().Return(a).Or(Parse.String("//{};").Token().Return(a)));

        public static Parser<IEnumerable<T>> OrEmpty<T>(this Parser<IOption<IEnumerable<T>>> p) => p.Select(v => v.IsDefined ? v.Get() : Array.Empty<T>());
        public static Parser<T> NotRepeated<T>(this Parser<T> p) => p.Then(val => p.Isol().Not().Return(val));
    }
}