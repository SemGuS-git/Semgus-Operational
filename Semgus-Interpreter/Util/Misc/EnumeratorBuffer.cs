using System;
using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util.Misc {
    public class EnumeratorBuffer<T> : IEnumerable<T> {
        private class Enumerator : IEnumerator<T> {
            readonly EnumeratorBuffer<T> parent;

            private int i = -1;

            public Enumerator(EnumeratorBuffer<T> parent) {
                this.parent = parent;
            }

            public T Current => parent.history[i];

            object IEnumerator.Current => Current;

            public bool MoveNext() {
                return parent.TryAdvanceTo(++i);
            }

            public void Dispose() { }

            public void Reset() => throw new NotSupportedException();
        }

        private readonly List<T> history = new();
        private readonly IEnumerator<T> source;

        public EnumeratorBuffer(IEnumerator<T> source) {
            this.source = source;
        }

        private bool TryAdvanceTo(int v) {
            while (history.Count < v) {
                if (source.MoveNext()) {
                    history.Add(source.Current);
                } else {
                    return false;
                }
            }
            return true;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
