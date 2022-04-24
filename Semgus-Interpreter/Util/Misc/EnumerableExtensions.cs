using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.Util.Misc {

    public static class EnumerableExtensions {
        public static (IEnumerator<T> when_true, IEnumerator<T> when_false) Partition<T>(this IEnumerator<T> enumerator, Func<T, bool> predicate)
            => PartitionHelper<T>.SplitEnumerator(enumerator, predicate);

        public static IEnumerable<T> Cached<T>(this IEnumerator<T> enumerator) => new EnumeratorBuffer<T>(enumerator);
        public static IEnumerable<T> Wrapped<T>(this IEnumerator<T> enumerator) => new EnumeratorWrapper<T>(enumerator);
        public static IReadOnlyList<T> ReadToList<T>(this IEnumerator<T> enumerator) {
            List<T> list = new();
            while (enumerator.MoveNext()) list.Add(enumerator.Current);
            return list;
        }

        public static (IReadOnlyList<T> a, IReadOnlyList<T> b) ReadToLists<T>(this (IEnumerator<T> a, IEnumerator<T> b) tue) => (tue.a.ReadToList(), tue.b.ReadToList());

        private class PartitionHelper<T> {
            class TrueEnumerator : IEnumerator<T> {
                public T Current => has ? stored : throw new InvalidOperationException();
                object IEnumerator.Current => Current;

                private T stored = default(T);
                private bool has = false;
                private PartitionHelper<T> parent;

                public TrueEnumerator(PartitionHelper<T> parent) => this.parent = parent;

                public void Dispose() { }
                public bool MoveNext() => has = parent.MoveNextTrue(out stored);
                public void Reset() => throw new NotSupportedException();
            }
            class FalseEnumerator : IEnumerator<T> {
                public T Current => has ? stored : throw new InvalidOperationException();
                object IEnumerator.Current => Current;

                private T stored = default(T);
                private bool has = false;
                private PartitionHelper<T> parent;

                public FalseEnumerator(PartitionHelper<T> parent) => this.parent = parent;

                public void Dispose() { }
                public bool MoveNext() => has = parent.MoveNextFalse(out stored);
                public void Reset() => throw new NotSupportedException();
            }

            private readonly Queue<T> trueQueue = new();
            private readonly Queue<T> falseQueue = new();

            private readonly IEnumerator<T> source;
            private readonly Func<T, bool> predicate;

            private PartitionHelper(IEnumerator<T> source, Func<T, bool> predicate) {
                this.source = source;
                this.predicate = predicate;
            }

            private bool MoveNextTrue(out T result) {
                if (trueQueue.TryDequeue(out result)) return true;
                while (source.MoveNext()) {
                    if (predicate(source.Current)) {
                        result = source.Current;
                        return true;
                    } else {
                        falseQueue.Enqueue(source.Current);
                    }
                }
                result = default;
                return false;
            }

            private bool MoveNextFalse(out T result) {
                if (falseQueue.TryDequeue(out result)) return true;
                while (source.MoveNext()) {
                    if (predicate(source.Current)) {
                        trueQueue.Enqueue(source.Current);
                    } else {
                        result = source.Current;
                        return true;
                    }
                }
                result = default;
                return false;
            }

            public static (IEnumerator<T> when_true, IEnumerator<T> when_false) SplitEnumerator(IEnumerator<T> source, Func<T, bool> predicate) {
                var a = new PartitionHelper<T>(source, predicate);
                return (new TrueEnumerator(a), new FalseEnumerator(a));
            }
        }
    }
}
