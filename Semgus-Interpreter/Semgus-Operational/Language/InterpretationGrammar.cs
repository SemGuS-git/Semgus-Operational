using Semgus.Util;
using System.Diagnostics;

namespace Semgus.Operational {
    /// <summary>
    /// Maps nonterminals to their sets of productions.
    /// </summary>
    public class InterpretationGrammar {
        public int RuleCount => Productions.ValueCount;

        public IReadOnlyCollection<NtSymbol> Nonterminals => _nonterminals;
        private readonly HashSet<NtSymbol> _nonterminals;

        public NtSymbol StartSymbol { get; }
        public DictOfList<NtSymbol, NonterminalProduction> Productions { get; }
        public DictOfList<NtSymbol, NtSymbol> PassthroughProductions { get; }

        public InterpretationGrammar(NtSymbol startSymbol, DictOfList<NtSymbol, NonterminalProduction> productions, DictOfList<NtSymbol, NtSymbol> passthroughProductions) {
            StartSymbol = startSymbol;
            Productions = productions;
            PassthroughProductions = passthroughProductions;
            _nonterminals = new(productions.Keys);

            Debug.Assert(_nonterminals.Contains(startSymbol));
            Debug.Assert(_nonterminals.SetEquals(passthroughProductions.Keys));
        }
    }
}