using Semgus.Model.Smt;

namespace Semgus {
    internal static class SmtIdentifierExtensions {
        public static string AsString(this SmtIdentifier a) => a.Indices.Length == 0 ? a.Symbol : throw new NotSupportedException();
    }
}
