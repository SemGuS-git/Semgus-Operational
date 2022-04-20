using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.SketchSyntax.Parsing.Iota {


    static class Parser3 {
        StreamAccumulator<ILexeme> Tokens { get; }




        static imut<INode> term3 = Term5;
        static imut<INode> term4 = Term5;
        static imut<INode> term5 = Term5;
        static imut<INode> term6 => term5.With(Trailing("||",term5), (head, tail) => tail.Count == 0 ? head : new InfixOperation(Op.Or, tail));


        static imut<INode> Infix(Op op, imut<INode> term) => term.Collect(Special(op.Str()).And(term).Any()).Select(list => list.Count == 1 ? List[0] : new InfixOperation(op, head, tail));


        static bool Term5(out INode n) => throw new NotImplementedException();

        static matcher Special(string s) => () => true;


        static imut<IReadOnlyList<INode>> Trailing(string s, imut<INode> tail) => Special(s).And(tail).Any().Collect();

        static bool Term6(out INode n) 

    }
    delegate bool imut<T>(out T node);
    delegate bool matcher();

    delegate T combinator<L, R, T>(L head, R tail);


    static class Ext {

        private class RepeatEnumerator<T> : IEnumerator<T> {
            public T Current => has ? _current : throw new InvalidOperationException();

            bool has = false;
            private T? _current = default;
            private imut<T> _thing;

            object IEnumerator.Current => Current;

            public RepeatEnumerator(imut<T> thing) {
                _thing = thing;
            }

            public void Dispose() { }

            public bool MoveNext() => (has = _thing(out _current));

            public void Reset() => throw new NotSupportedException();
        }

        private class Wrap<T> : IEnumerable<T> {
            private Func<IEnumerator<T>> _generator;

            public Wrap(Func<IEnumerator<T>> generator) {
                _generator = generator;
            }

            public IEnumerator<T> GetEnumerator() => _generator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static imut<IEnumerable<T>> Any<T>(this imut<T> a) {
            bool ok(out IEnumerable<T> stream) {
                stream = new Wrap<T>(() => new RepeatEnumerator<T>(a));
                return true;
            }
            return ok;
        }

        public static imut<T> And<T>(this matcher head, imut<T> tail) {
            bool result(out T node) {
                if (head()) return tail(out node);
                node = default;
                return false;
            }
            return result;
        }
        public static imut<IReadOnlyList<T>> Collect<T>(this imut<T> head, imut<IEnumerable<T>> tail) { 
            bool result(out IReadOnlyList<T> node) {
                if (head(out var a) && tail(out var b)) {
                    var l = new List<T>() { a };
                    l.AddRange(b);
                    node = l;
                    return true;
                } else {
                    node = default;
                    return false;
                }
            }
            return result;
        }

        public static imut<IReadOnlyList<T>> Collect<T>(this imut<IEnumerable<T>> src) {
            bool result(out IReadOnlyList<T> node) {
                if (src(out var e)) {
                    node = e.ToList();
                    return true;
                } else {
                    node = default;
                    return false;
                }
            }
            return result;
        }


        public static imut<(T0, T1)> And<T0, T1>(this imut<T0> head, imut<T1> tail) => head.With(tail, (a, b) => (a, b));
        public static imut<(T0, T1, T2)> And<T0, T1, T2>(this imut<(T0, T1)> head, imut<T2> tail) => head.With(tail, (a, b) => (a.Item1, a.Item2, b));

        public static imut<T> With<L, R, T>(this imut<L> head, imut<R> tail, combinator<L, R, T> combinator) {
            bool result(out T node) {
                if (!head(out var v_head)) {
                    node = default;
                    return false;
                }
                if (!tail(out var v_tail)) {
                    node = default;
                    return false;
                }

                node = combinator(v_head, v_tail);
                return true;

            }
            return result;
        }
    }

    class Parser {

        readonly StreamAccumulator<ILexeme> lexemes;

        public Parser(IEnumerable<ILexeme> source) {
            lexemes = new(source.GetEnumerator());
        }

        void ParseNext() {
            int cursor = 0;
            lexemes.TryGet(cursor, out var first);


        }
        interface ITokenTwo { }


        interface Item : ITokenTwo { }
        interface Dottable : Item {

        }

        class Keyword : Item, ITokenTwo {

        }

        class Access : ITokenTwo {

        }

        bool Matches(int i, params string[] s) => lexemes.TryGet(i, out var lex) && s.Length switch {
            0 => false,
            1 => lex.Matches(s[0]),
            _ => lex.MatchesAny(new HashSet<string>(s), out _),
        };

        bool Match(int i, string s) => lexemes.TryGet(i, out var lex) && lex.Matches(s);
        bool Match(int i, char c) => lexemes.TryGet(i, out var lex) && lex.Matches(c);

        bool TryMatch(int i, HashSet<string> opt, out string? which) {
            if (lexemes.TryGet(i, out var lex)) return lex.MatchesAny(opt, out which);
            else {
                which = default;
                return false;
            }
        }

        class Parenthetical : Dottable {
            public List<List<Item>> CommaDelimited { get; } = new();

            public Parenthetical(List<List<Item>> el) {
                this.CommaDelimited = el;
            }
        }

        bool TryItemSequence(int i, out (int, List<Item>) found) {
            List<Item> items = new();

            while (TryItem(i, out var finding)) {
                (i, var item) = finding;
                items.Add(item);
            }

            if (items.Count == 0) {
                found = default;
                return false;
            } else {
                found = (i, new(items));
                return true;
            }
        }

        bool IsChar(int i, char c) {
            return lexemes[i] is SpecialChar sc && sc.Value == c;
        }
        bool Ok(int i) => lexemes.Ok(i);

        bool TryParenthetical(int i, out (int, Parenthetical) found) {
            List<List<Item>> parts = new();

            while (TryItemSequence(i, out var finding)) {
                (i, var part) = finding;
                parts.Add(part);

                if (Match(i, ',')) i++;
                else break;
            }

            if (Match(i++, ')')) {
                found = (i, new(parts));
                return true;
            }
            found = default;
            return false;
        }

        IReadOnlyDictionary<string, Keyword> keywords;

        class IdentifierToken : Dottable {
            public IdentifierToken(string value) {
                Value = value;
            }

            public string Value { get; }
        }

        class Access : Dottable {
            public Access(Dottable root, IdentifierToken property) {
                Root = root;
                Property = property;
            }

            public Dottable Root { get; }
            public IdentifierToken Property { get; }

        }

        class Operator : Item {
            public Operator(string? op) {
                Op = op;
            }

            public string? Op { get; }
        }

        class BraceBlock : Procedural {
            public BraceBlock(List<Item> head, List<Procedural> parts) {
                Head = head;
                Parts = parts;
            }

            public List<Item> Head { get; }
            public List<Procedural> Parts { get; }
            List<Procedural> Contents { get; }
        }

        interface Procedural : ITokenTwo {

        }

        class LineStatement : Procedural {
            private List<Item> head;

            public LineStatement(List<Item> head) {
                this.head = head;
            }
        }

        bool TryProcedural(int i, out (int, Procedural) found) {
            List<Item> head;
            {
                if (!TryItemSequence(i, out var finding)) {
                    found = default;
                    return false;
                }
                (i, var head) = finding;
            }


            if (!lexemes.TryGet(i, out var lex)) {
                found = default;
                return false;
            }

            if (lex.Matches(';')) {
                found = new(i + 1, new LineStatement(head));
                return true;
            }

            if (lex.Matches('{')) {
                List<Procedural> parts = new();
                i++;
                while (TryProcedural(i, out var inner)) {
                    (i, var next) = inner;
                    parts.Add(next);
                }

                if (Match(i, '}')) {
                    found = (i + 1, new BraceBlock(head, parts));
                    return true;
                }
            }

            found = default;
            return false;
        }

        bool TryItem(int i, out (int, Item) found) {
            if (!lexemes.TryGet(i, out var lex)) {
                found = default;
                return false;
            }

            if (lex.MatchesAny(_operators, out var op)) {
                found = (i + 1, new Operator(op));
                return true;
            }

            if (lex is Word word) {
                if (keywords.TryGetValue(word.Value, out var kw)) {
                    found = (i + 1, kw);
                } else {
                    found = (i + 1, new IdentifierToken(word.Value));
                }
                return true;
            }

            if (lex is Item native_item) {
                found = (i + 1, native_item);
                return true;
            }

            if (TryDottable(i, out var finding)) {
                (i, Dottable obj) = finding;

                // Access
                while (Match(++i, '.')) {
                    if (TryIdentifier(i + 1, out var propId)) {
                        obj = new Access(obj, propId);
                        i++;
                    } else {
                        found = default;
                        return false;
                    }
                }
                found = (i, obj);
                return true;
            }

            found = default;
            return false;
        }

        private static readonly HashSet<string> _operators = "= + - * < > ! == != || && <= >=".Split(' ').ToHashSet();


        bool TryDottable(int i, out (int, Dottable) found) {
            if (!lexemes.TryGet(i, out var lex)) {
                found = default;
                return false;
            }



            if (lex.Matches('(')) return TryParenthetical(i + 1, out found);

            if (TryIdentifier(lex, out var id)) {
                found = (i + 1, id);
                return true;
            } else {
                found = default;
                return false;
            }
        }

        private bool TryIdentifier(int i, out IdentifierToken id) {
            if (lexemes.TryGet(i, out var lex) && TryIdentifier(lex, out id)) return true;
            id = default;
            return false;
        }

        private bool TryIdentifier(ILexeme lex, out IdentifierToken id) {
            if (lex is Word word && !keywords.ContainsKey(word.Value)) {
                id = new(word.Value);
                return true;
            } else {
                id = default;
                return false;
            }
        }
    }


    internal static class TokenExtensions {
        public static bool Is(this ILexeme token, char v) => token is SpecialChar a && a.Value == v;
        public static bool Is(this ILexeme token, string v) => token is SpecialMore a && a.Value == v;
    }

    internal struct LineComment : ILexeme {
        public readonly string Value;

        public LineComment(string value) {
            this.Value = value;
        }
    }

    internal struct BlockComment : ILexeme {
        public readonly string Value;

        public BlockComment(string value) {
            this.Value = value;
        }
    }


    internal struct SpecialChar : ILexeme {

        public readonly char Value;

        public SpecialChar(char value) {
            Value = value;
        }

        public static (char, ILexeme) Of(char value) => (value, new SpecialChar(value));

        public bool Matches(string s) => s.Length == 1 && Value == s[0];
        public bool Matches(char s) => Value == s;
        public bool MatchesAny(HashSet<string> s, out string? which) {
            var val_s = new string(Value, 1);
            if (s.Contains(val_s)) {
                which = val_s;
                return true;
            } else {
                which = null;
                return false;
            }
        }
    }
    internal struct SpecialMore : ILexeme {

        public readonly string Value;

        public SpecialMore(string value) {
            Value = value;
        }
        public static (string, ILexeme) Of(string value) => (value, new SpecialMore(value));

        public bool Matches(string s) => Value == s;
        public bool Matches(char s) => Value.Length == 1 && Value[0] == s;
    }

    internal struct None : ILexeme { }

    internal struct Word : ILexeme {
        public readonly string Value;

        public Word(string value) {
            this.Value = value;
        }

        public bool Matches(string s) => Value == s;

        public bool Matches(char s) => Value.Length == 1 && Value[0] == s;

        public bool MatchesAny(HashSet<string> s, out string? which) {
            if (s.Contains(Value)) {
                which = Value;
                return true;
            } else {
                which = null;
                return false;
            }
        }
    }

    internal struct Literal : ILexeme {
        public readonly int Value;

        public Literal(int value) {
            Value = value;
        }

        public static Literal Zero { get; } = new(0);

    }


    internal interface ILexeme {
        bool Matches(string s) => false;
        bool Matches(char s) => false;

        bool MatchesAny(HashSet<string> s, out string? which) {
            which = null;
            return false;
        }
    }
}
