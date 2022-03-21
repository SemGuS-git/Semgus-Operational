using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util {
    /// <summary>
    /// Singleton class to use when returning an empty collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptyCollection<T> : IReadOnlyCollection<T> {
        public static EmptyCollection<T> Instance { get; } = new EmptyCollection<T>();

        public int Count => 0;

        public IEnumerator<T> GetEnumerator() => EmptyEnumerator<T>.Instance;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
