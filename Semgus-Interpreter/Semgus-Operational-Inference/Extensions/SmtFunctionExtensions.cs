using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;

namespace Semgus {
    internal static class SmtFunctionExtensions {
        public static bool IsEquality(this SmtFunction f) => f.Name == SmtCommonIdentifiers.FN_EQ;
        public static bool IsConjunction(this SmtFunction f) => f.Name == SmtCommonIdentifiers.FN_AND;

        public static string StringName(this SmtFunction a) => a.Name.AsString();

    }
}
