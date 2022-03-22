using Semgus.Operational;
using Semgus.Util;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Solvers.Enumerative {
    public class ExpressionBank {
        private readonly AutoDict<NtSymbol, DictOfList<int, IDSLSyntaxNode>> _dict
            = new AutoDict<NtSymbol, DictOfList<int, IDSLSyntaxNode>>(_ => new DictOfList<int, IDSLSyntaxNode>());

        public int Size => _dict.Values.Select(k => k.Values.Select(v => v.Count).Sum()).Sum();

        public void Add(NtSymbol nt, int cost, IDSLSyntaxNode expr) {
            _dict.SafeGet(nt).SafeGetCollection(cost).Add(expr);
        }

        public IReadOnlyList<DictOfList<int, IDSLSyntaxNode>> GetCandidateSets(IReadOnlyList<NtSymbol> slotNonterminals) {
            int n = slotNonterminals.Count;
            DictOfList<int, IDSLSyntaxNode>[] array = new DictOfList<int, IDSLSyntaxNode>[n];
            for (int i = 0; i < n; i++) {
                array[i] = _dict.SafeGet(slotNonterminals[i]);
            }
            return array;
        }
    }
}