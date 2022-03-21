using System;
using System.Collections;
using System.Collections.Generic;

namespace Semgus.Util {
    /// <summary>
    /// Singleton class to use when returning an empty enumerator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptyEnumerator<T> : IEnumerator<T> {
        public static EmptyEnumerator<T> Instance { get; } = new EmptyEnumerator<T>();

        public T Current => throw new InvalidOperationException();

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext() => false;

        public void Reset() { }
    }
}
