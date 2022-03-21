using System;
using System.Collections.Generic;

namespace Semgus.Util {
    public class Counter<TKey> {
        private readonly AutoDict<TKey, int> _dict = new AutoDict<TKey, int>(_ => 0);

        public int Peek(TKey key) => _dict.TryGetValue(key, out var value) ? value : 0;

        public int Increment(TKey key) {
            var i = _dict.SafeGet(key);
            i += 1;
            _dict[key] = i;
            return i;
        }

    }

    /// <summary>
    /// Dictionary equipped with a generator function for mapping new keys to new values.
    /// </summary>
    public class AutoDict<TKey, TValue> : Dictionary<TKey,TValue> {
        private readonly Func<TKey, TValue> _ctor;

        public AutoDict(Func<TKey, TValue> ctor) : base() {
            this._ctor = ctor;
        }

        public TValue SafeGet(TKey key) {
            TValue value;
            if (!TryGetValue(key, out value)) {
                value = _ctor(key);
                Add(key, value);
            }
            return value;
        }
    }
}
