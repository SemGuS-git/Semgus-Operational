using System;
using System.Collections.Generic;

namespace Semgus.Util {
    public class CompositeDisposable : IDisposable {
        private readonly List<IDisposable> _items = new();
        private bool _disposed;

        public CompositeDisposable(params IDisposable[] items) {
            _items.AddRange(items);
        }

        public void Add(IDisposable item) {
            if(_disposed) {
                item.Dispose();
            } else {
                _items.Add(item);
            }
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            foreach(var item in _items) {
                item.Dispose();
            }
        }
    }
}
