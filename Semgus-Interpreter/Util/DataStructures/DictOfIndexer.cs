using System.Collections.Generic;
using System.Linq;

namespace Semgus.Util {
    /// <summary>
    /// A dictionary of <see cref="IndexCollection{TValue}"/>.
    /// Associates each (TKey, int) with a list of TValue.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictOfIndexer<TKey, TValue> {
        private readonly AutoDict<TKey, IndexCollection<TValue>> _dict = new AutoDict<TKey, IndexCollection<TValue>>(_ => new IndexCollection<TValue>());

        public int ValueCount => _dict.Values.Select(v => v.ValueCount).Sum();

        public void Add(TKey key, int index, TValue value) => _dict.SafeGet(key).Add(index, value);

        public IReadOnlyCollection<TValue> GetValues(TKey key, int index) => _dict[key].GetValues(index);

        public IndexCollection<TValue> SafeGetIndexer(TKey key) => _dict.SafeGet(key);

        public IEnumerable<TValue> EnumerateAll() => _dict.Values.SelectMany(v => v.EnumerateAll());
    }
}
