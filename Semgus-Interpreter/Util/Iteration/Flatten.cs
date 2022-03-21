using System;
using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util {
    public class Flatten<T> : IEnumerable<T> {
        private readonly List<IReadOnlyList<T>> _lists = new List<IReadOnlyList<T>>();

        public void Add(IReadOnlyList<T> list) => _lists.Add(list);

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        private class Enumerator : IEnumerator<T> {
            private readonly Flatten<T> obj;

            int _i = 0;
            int _j = -1;

            public Enumerator(Flatten<T> obj) {
                this.obj = obj;
            }

            public T Current => obj._lists[_i][_j];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext() {
                int n = obj._lists.Count;
                if (_i >= n) return false;
                var target = obj._lists[_i];

                while(++_j >= target.Count) {
                    if (++_i >= n) return false;
                    _j = -1;
                    target = obj._lists[_i];
                }

                return true;
            }

            public void Reset() {
                _i = 0;
                _j = -1;
            }
        }
    }
}
