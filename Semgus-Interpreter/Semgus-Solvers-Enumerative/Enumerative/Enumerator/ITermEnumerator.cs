using System.Collections.Generic;
using Semgus.Operational;

namespace Semgus.Solvers.Enumerative {
    public interface ITermEnumerator {
        IEnumerable<IDSLSyntaxNode> EnumerateAtCost(int value);
        int GetHighestAvailableCost();
    }
}