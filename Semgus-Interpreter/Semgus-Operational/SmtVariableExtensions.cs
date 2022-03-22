using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;

namespace Semgus {
    public static class SmtVariableExtensions {
        public static bool MatchesId(this SmtVariable a, SmtVariable b) => a.Name == b.Name;
        public static bool MatchesId(this SmtVariable a, SmtVariableBinding b) => a.Name == b.Id;
        public static string StringName(this SmtVariable a) => a.Name.AsString();
    }
}
