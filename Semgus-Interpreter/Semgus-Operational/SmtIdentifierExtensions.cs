using Semgus.Model.Smt;

namespace Semgus {
    public static class SmtSortIdentifierExtensions {
        public static string AsString(this SmtSortIdentifier a) => a.Arity==0 ? a.Name.AsString() : throw new NotSupportedException();
    }

    public static class SmtIdentifierExtensions {
        public static string AsString(this SmtIdentifier a) => a.Indices.Length == 0 ? a.Symbol : throw new NotSupportedException();
    }
}
