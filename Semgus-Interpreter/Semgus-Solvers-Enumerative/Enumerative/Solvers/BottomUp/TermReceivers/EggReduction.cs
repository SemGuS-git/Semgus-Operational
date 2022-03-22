using Microsoft.Extensions.Logging;
using Semgus.Operational;
using System;
using System.Collections.Generic;

namespace Semgus.Solvers.Enumerative {
    public class EggReduction : IReduction, IDisposable {
#if USE_RUST
        private readonly HashSet<string> _eggUniques = new();
        private readonly RustToolsHandle _tools;

        public ILogger Logger { get; set; }

        public EggReduction(IEnumerable<string> rules) {
            _tools = RustToolsHandle.Create(string.Join('\n', rules));
        }

        public bool CanPrune(IDSLSyntaxNode node) {
            var input = node.ToString();
            var output = _tools.DoReduce(input);
            if (_eggUniques.Add(output)) {
                Logger?.LogTrace("New unique {a}", output);
                return false;
            } else {
                Logger?.LogTrace("Discard nonunique {a} -> {b}", input, output);
                return true;
            }
        }

        public void Dispose() => _tools.Dispose();
#else
        public EggReduction(IEnumerable<string> rules) => throw new NotImplementedException();

        public ILogger Logger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanPrune(IDSLSyntaxNode node) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
#endif
    }
}
