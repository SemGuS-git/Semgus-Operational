using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.Subproblems.LatticeSubstep {
    internal interface ILatticeSubstep {
        Identifier TargetId { get; }
        StructType Subject { get; }
        IEnumerable<IStatement> GetInitialFile();
        IEnumerable<IStatement> GetRefinementFile(FunctionDefinition prev);
    }
}