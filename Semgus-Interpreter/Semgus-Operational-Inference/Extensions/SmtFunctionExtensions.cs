using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;

namespace Semgus {
    internal static class SmtFunctionExtensions {
        public static bool IsEquality(this SmtFunction f) => f.Name == SmtCommonIdentifiers.EqFunctionId;
        public static bool IsConjunction(this SmtFunction f) => f.Name == SmtCommonIdentifiers.AndFunctionId;

        public static string StringName(this SmtFunction a) => a.Name.AsString();

    }
}
