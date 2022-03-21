using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util {
    public class UniqueQueue<T> : IReadOnlyCollection<T> {
        private readonly Queue<T> _queue = new();
        private readonly HashSet<T> _guard = new();

        public UniqueQueue() { }

        public UniqueQueue(IEnumerable<T> values) => EnqueueRange(values);

        public bool Enqueue(T value) {
            if (_guard.Add(value)) {
                _queue.Enqueue(value);
                return true;
            }
            return false;
        }

        public T Dequeue() {
            var a = _queue.Dequeue();
            _guard.Remove(a);
            return a;
        }

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
        public int Count => _queue.Count;

        public void EnqueueRange(IEnumerable<T> values) {
            foreach (var value in values) Enqueue(value);
        }
    }

}
