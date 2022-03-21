using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Util {
    /// <summary>
    /// Dictionary containing collections of values.
    /// Takes care of the boilerplate code for adding/removing collections as individual elements
    /// are added/removed.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TCollection"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictOfCollection<TKey, TCollection, TValue> : IReadOnlyDictionary<TKey,TCollection> where TCollection : ICollection<TValue> {
        private readonly AutoDict<TKey, TCollection> _dict;

        public IEnumerable<TKey> Keys => _dict.Keys;
        public IEnumerable<TCollection> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public TCollection this[TKey key] => _dict[key];
        public bool TryGetValue(TKey key, out TCollection collection) => _dict.TryGetValue(key, out collection);
        public IEnumerator<KeyValuePair<TKey, TCollection>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int ValueCount => _dict.Values.Sum(v => v.Count);

        public DictOfCollection(Func<TKey, TCollection> ctor) {
            this._dict = new AutoDict<TKey, TCollection>(ctor);
        }

        public DictOfCollection() {
        }

        public IEnumerable<(TKey, TValue)> EnumerateKeyElementTuples() {
            foreach (var kvp in _dict) {
                foreach (var item in kvp.Value) {
                    yield return (kvp.Key, item);
                }
            }
        }

        public IEnumerable<TValue> EnumerateAllValues() => _dict.Values.SelectMany(v => v);

        public TCollection SafeGetCollection(TKey key) => _dict.SafeGet(key);

        public void AddCollection(TKey key, TCollection collection) => _dict.Add(key, collection);
        public void Add(TKey key, TValue value) => _dict.SafeGet(key).Add(value);

        public bool RemoveCollection(TKey key) => _dict.Remove(key);
        public bool Remove(TKey key, TValue value) {
            TCollection collection;
            if (!_dict.TryGetValue(key, out collection)) {
                return false;
            }
            var success = collection.Remove(value);
            if (success && collection.Count == 0) _dict.Remove(key);
            return success;
        }

        public void Clear() => _dict.Clear();
    }
}
