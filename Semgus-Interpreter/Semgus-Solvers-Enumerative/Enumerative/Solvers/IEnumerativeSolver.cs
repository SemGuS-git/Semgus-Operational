using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Operational;

namespace Semgus.Solvers.Enumerative {
    public interface IEnumerativeSolver {
        ILogger Logger { get; set; }
        ISynthesisResult Run(InterpretationGrammar grammar, InductiveConstraint spec);
    }
}
