namespace Semgus.OrderSynthesis.SketchSyntax.Parsing.Iota {
    internal class Trie<T> {
        public T? Value;
        private Dictionary<char, Trie<T>> branches = new();

        public Trie<T> Get(char c) => branches![c];

        public bool TryGet(char c, out Trie<T> t) => branches.TryGetValue(c, out t);

        public Trie<T> Branch(char c) {
            if (!branches.TryGetValue(c, out var t)) {
                t = new();
                branches[c] = t;
            }
            return t;
        }

        public void Put(T value) {
            if (Value is not null) throw new InvalidOperationException();
            Value = value;
        }

        public void Insert(char c, T value) {
            Branch(c).Put(value);
        }

        public void Insert(string s, T value) {
            var t = this;
            foreach (var c in s) {
                t = t.Branch(c);
            }
            t.Put(value);
        }

        public static Trie<T> Build(IEnumerable<(char, T)> at_chars, IEnumerable<(string, T)> at_strings) {
            var root = new Trie<T>();
            foreach (var (c, t) in at_chars) root.Insert(c, t);
            foreach (var (s, t) in at_strings) root.Insert(s, t);
            return root;
        }
    }
}
