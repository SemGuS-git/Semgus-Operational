using System.Collections.Generic;
using System.Linq;

namespace Semgus.Util {
    /// <summary>
    /// Associates an int index with a list of TValue.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class IndexCollection<TValue> {
        private readonly List<List<TValue>> _list = new List<List<TValue>>();

        public int Height => _list.Count;
        public int ValueCount => _list.Select(l=>l.Count).Sum();

        public void Add(int index, TValue value) {
            while (_list.Count <= index) {
                _list.Add(new List<TValue>());
            }
            _list[index].Add(value);
        }
        
        public IReadOnlyCollection<TValue> GetValues(int index) => _list[index];

        public IReadOnlyCollection<TValue> SafeGetValues(int index) =>
            index < _list.Count ? _list[index] : EmptyCollection<TValue>.Instance;

        public IEnumerable<TValue> EnumerateAll() => _list.SelectMany(l => l);
    }
}
