using System.Diagnostics;

namespace Semgus.OrderSynthesis.AbstractInterpretation {

    internal class EquivalenceClasses<TKey, TValue> {
        private readonly List<TValue> _values = new();
        private readonly Dictionary<TKey, int> _keyToIndex = new();
        private readonly Dictionary<int, HashSet<TKey>> _indexToEClass = new();

        public void Add(TKey key, TValue value) {
            int k = _values.Count;
            _values.Add(value);
            _keyToIndex.Add(key, k);
            _indexToEClass.Add(k, new() { key });
        }

        public void Merge(TKey a, TKey b) {
            var n_a = _keyToIndex[a];
            var n_b = _keyToIndex[b];

            if (n_a == n_b) return;

            _keyToIndex[b] = n_a;

            if (_indexToEClass.Remove(n_b, out var was)) {
                var etc = _indexToEClass[n_a];
                foreach (var t in was) etc.Add(t);
            }
        }

        public bool Remove(TKey key) {
            // remove key's index map entry
            if (!_keyToIndex.Remove(key, out var idx)) return false;

            // remove key from its e-class
            var eclass = _indexToEClass[idx];
            Debug.Assert(eclass.Remove(key));

            // remove empty e-classes
            if (eclass.Count == 0) _indexToEClass.Remove(idx);

            return true;
        }

        public TValue this[TKey key] {
            get => _values[_keyToIndex[key]];
        }

        public IEnumerable<(IReadOnlyCollection<TKey> keys, TValue value)> Enumerate() {
            foreach (var (index, keys) in _indexToEClass) {
                yield return (keys, _values[index]);
            }
        }
    }

    // todo move to Util
    internal class AliasMap<TKey> {
        private Dictionary<TKey, TKey> _map = new();

        public bool IsAlias(TKey key) => _map.ContainsKey(key);

        public TKey Resolve(TKey alias) => TryGet(alias, out var value) ? value : alias;

        public bool TryGet(TKey alias, out TKey value) {
            bool ok = false;
            while (_map.TryGetValue(alias, out var next)) {
                ok = true;
                alias = next;
            }
            value = ok ? alias : default;
            return ok;
        }

        public void Register(TKey alias, TKey value) {
            if (alias.Equals(value)) return;

            while (_map.TryGetValue(value, out var next)) {
                if (alias.Equals(next)) return; // ok
                value = next;
            }

            // if there's already a target for this alias, make sure it's the same thing
            if (TryGet(alias, out var other)) {
                if (!value.Equals(other)) throw new ArgumentException("Key already mapped to something else");
                return;
            }

            _map.Add(alias, value);
        }
    }



    //internal class Agglom {
    //    SemgusTermType TermType { get; }
    //    SynthComparisonFunction CompareIn { get; }
    //    SynthComparisonFunction CompareOut { get; }
    //}

    //internal class Interval {

    //}
    //internal class SemRelArgTupleType {

    //}

    //internal class SynthComparisonFunction {
    //    public bool Leq() {

    //    }
    //}
}
