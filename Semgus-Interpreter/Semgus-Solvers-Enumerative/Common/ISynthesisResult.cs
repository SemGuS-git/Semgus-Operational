using Semgus.Operational;
using System;

namespace Semgus.Solvers {
    public interface ISynthesisResult {
        SolverInputInfo InputInfo { get; set; }
        SynthesisStopCode StopCode { get; }
        IDSLSyntaxNode Program { get; }
        TimeSpan Runtime { get; }
    }
}
