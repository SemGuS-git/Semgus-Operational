using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Interpretation {
    public interface ITheoryImplementation {
        public bool TryGetFunction(SmtFunction def, SmtFunctionRank rank, [NotNullWhen(true)] out FunctionInstance? fn);
    }
}
