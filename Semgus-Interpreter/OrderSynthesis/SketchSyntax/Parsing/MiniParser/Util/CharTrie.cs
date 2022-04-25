namespace Semgus.Util {
    internal class CharTrie<T> {
        public T? Value;
        private readonly Dictionary<char, CharTrie<T>> branches = new();

        public CharTrie<T> Get(char c) => branches[c];

        public bool TryGet(char c, out CharTrie<T> t) => branches.TryGetValue(c, out t);

        public CharTrie<T> Branch(char c) {
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

        public static CharTrie<T> Build(IEnumerable<(string, T)> at_strings) {
            var root = new CharTrie<T>();
            foreach (var (s, t) in at_strings) root.Insert(s, t);
            return root;
        }
    }
}
