using Semgus.Interpretation;
using static Semgus.Model.SemgusGrammar;

namespace Semgus {
    internal static class NonTerminalExtensions {
        public static Nonterminal Convert(this NonTerminal a) => new(a.Name.AsString());
    }
}
