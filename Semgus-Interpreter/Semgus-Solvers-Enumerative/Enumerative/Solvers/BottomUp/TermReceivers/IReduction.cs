using Microsoft.Extensions.Logging;
using Semgus.Operational;

namespace Semgus.Solvers.Enumerative {
    public interface IReduction {
        public ILogger Logger { get; set; }
        bool CanPrune(IDSLSyntaxNode node);
    }
}
