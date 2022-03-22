using System;
using System.Diagnostics;

namespace Semgus.Solvers.Enumerative {
    public class StopCondition {
        private readonly TimeSpan _timeout;
        private readonly Stopwatch _stopwatch;

        public StopCondition(TimeSpan span) {
            this._timeout = span;
            this._stopwatch = new Stopwatch();
        }

        public void Start() {
            _stopwatch.Start();
        }

        public bool IsStop() => _stopwatch.Elapsed > _timeout;
    }
}