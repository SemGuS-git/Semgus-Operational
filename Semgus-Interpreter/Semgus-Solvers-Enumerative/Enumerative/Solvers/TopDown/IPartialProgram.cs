using Semgus.Operational;

namespace Semgus.Solvers.Enumerative {
    public interface IPartialProgram : IDSLSyntaxNode {
        void SetParent(PartialProgramNode parent);
        IPartialProgram DownClone();
    }
}