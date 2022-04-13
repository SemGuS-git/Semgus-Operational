using Semgus.Model;

namespace Semgus.Operational {
    public class SolutionBlock {
        public SemgusSynthFun SynthFun { get; }
        public IReadOnlyList<IDSLSyntaxNode> Solutions { get; }
        public SolutionBlock(SemgusSynthFun synthFun, IReadOnlyList<IDSLSyntaxNode> solutions) {
            SynthFun = synthFun;
            Solutions = solutions;
        }
    }
}