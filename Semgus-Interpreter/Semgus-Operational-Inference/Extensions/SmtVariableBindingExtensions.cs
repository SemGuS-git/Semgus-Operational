using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;

namespace Semgus {
    internal static class SmtVariableBindingExtensions {
        public static bool MatchesId(this SmtVariableBinding a, SmtVariable b) => a.Id == b.Name;
        public static bool MatchesId(this SmtVariableBinding a, SmtVariableBinding b) => a.Id == b.Id;
        public static string StringName(this SmtVariableBinding a) => a.Id.AsString();
    }
}
