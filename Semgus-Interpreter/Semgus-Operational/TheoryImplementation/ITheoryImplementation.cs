using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Operational {
    public interface ITheoryImplementation {
        public bool TryGetFunction(SmtFunction def, SmtFunctionRank rank, [NotNullWhen(true)] out FunctionInstance? fn);
    }
}
