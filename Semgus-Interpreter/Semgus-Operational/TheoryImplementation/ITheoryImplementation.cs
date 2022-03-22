using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;
using Semgus.Model.Smt.Terms;
using Semgus.Util;

namespace Semgus.Operational {
    public interface ITheoryImplementation {
        bool TryGetFunction(SmtFunction def, SmtFunctionRank rank, [NotNullWhen(true)] out FunctionInstance? fn);
        bool TryGetFunction(SmtIdentifier id, IEnumerable<SmtSort> argSorts, [NotNullWhen(true)] out SmtSort? returnSort, [NotNullWhen(true)] out FunctionInstance? fn);
    }
}
