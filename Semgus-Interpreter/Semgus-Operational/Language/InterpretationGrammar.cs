using Semgus.Util;

namespace Semgus.Operational {
    /// <summary>
    /// Maps nonterminals to their sets of productions.
    /// </summary>
    public class InterpretationGrammar {
        public int RuleCount => Productions.ValueCount;

        public IReadOnlyCollection<NtSymbol> Nonterminals => _nonterminals;
        private readonly HashSet<NtSymbol> _nonterminals;

        public DictOfList<NtSymbol, NonterminalProduction> Productions { get; }

        public InterpretationGrammar(DictOfList<NtSymbol, NonterminalProduction> productions) {
            _nonterminals = new(productions.Keys);
            Productions = productions;
        }
    }
}