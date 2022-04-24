using System;
using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util.Misc {
    public class EnumeratorWrapper<T> : IEnumerable<T> {
        private readonly IEnumerator<T> source;
        private bool didEmit = false;

        public EnumeratorWrapper(IEnumerator<T> source) {
            this.source = source;
        }

        public IEnumerator<T> GetEnumerator() => (!didEmit && (didEmit = true)) ? source : throw new InvalidOperationException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
