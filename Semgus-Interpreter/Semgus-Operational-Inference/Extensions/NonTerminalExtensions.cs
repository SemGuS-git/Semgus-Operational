using Semgus.Operational;
using static Semgus.Model.SemgusGrammar;

namespace Semgus {
    internal static class NonTerminalExtensions {
        public static NtSymbol Convert(this NonTerminal a) => new(a.Name.AsString());
    }
}
