using System;
using System.Collections.Generic;

namespace Semgus.Solvers.Enumerative {
    public class ConfigParameters {
        public TermCostFunction CostFunction { get; set; }
        public int? MaxCost { get; set; } = null;
        public TimeSpan? Timeout { get; set; }
        public int InterpreterMaxDepth { get; set; } = SolverDefaults.INTERPRETER_MAX_DEPTH;
        public List<ReductionMethod> Reductions { get; set; }
        public List<string> RewriteRules { get; set; }
    }
}
