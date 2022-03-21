using System.Collections.Generic;

namespace Semgus.Util {
    /// <summary>
    /// Tree data structure that stores sequences of values.
    /// Can be used like a <see cref="HashSet{IEnumerable{T}}"/>.
    /// Implemented as a <see cref="SequenceHashTreeMap{T, Unit}"/> to avoid code redundancy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SequenceHashTree<T> {
        private readonly SequenceHashTreeMap<T, Unit> _innerTree = new SequenceHashTreeMap<T, Unit>();

        public IEnumerable<IReadOnlyList<T>> EnumerateSequences() {
            foreach(var (seq, _) in _innerTree.EnumerateSequences()) {
                yield return seq;
            }
        }

        public bool TryAdd(IEnumerable<T> sequence) {
            return _innerTree.TryAdd(sequence, default);
        }

        public void Clear() => _innerTree.Clear();
    }
}
