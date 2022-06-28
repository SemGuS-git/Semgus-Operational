using System.Collections.Generic;
using Semgus.Operational;

namespace Semgus.Solvers.Enumerative {
    public interface ITermEnumerator {
        IReadOnlyDictionary<string, HashSet<NtSymbol>> GetTermTypeToNtMap();
        IEnumerable<IDSLSyntaxNode> EnumerateAtCost(int value);
        int GetHighestAvailableCost();
    }
}