using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;

namespace Semgus {
    internal static class SemgusChcExtensions {
        public static bool IsInputVar(this SemgusChc chc, SmtVariable v) => chc.InputVariables.Any(iv => iv.MatchesId(v));
        public static bool IsInputVar(this SemgusChc chc, SmtVariableBinding v) => chc.InputVariables.Any(iv => iv.MatchesId(v));
        public static bool IsOutputVar(this SemgusChc chc, SmtVariable v) => chc.OutputVariables.Any(iv => iv.MatchesId(v));
        public static bool IsOutputVar(this SemgusChc chc, SmtVariableBinding v) => chc.OutputVariables.Any(iv => iv.MatchesId(v));

    }
}
