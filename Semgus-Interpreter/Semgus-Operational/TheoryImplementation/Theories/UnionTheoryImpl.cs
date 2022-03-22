using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Operational {
    public class UnionTheoryImpl : ITheoryImplementation {
        private List<ITheoryImplementation> _members;

        public UnionTheoryImpl(IEnumerable<ITheoryImplementation> members) {
            this._members = members.ToList();
        }

        public bool TryGetFunction(SmtFunction def, SmtFunctionRank rank, [NotNullWhen(true)] out FunctionInstance? fn) {
            var any = false;
            FunctionInstance? found = default;

            foreach (var theory in _members) {
                if (theory.TryGetFunction(def, rank, out var temp)) {
                    if (any) {
                        throw new Exception("Ambiguous theory function match");
                    } else {
                        any = true;
                        found = temp;
                    }
                }
            }
            fn = found;
            return any;
        }
    }
}