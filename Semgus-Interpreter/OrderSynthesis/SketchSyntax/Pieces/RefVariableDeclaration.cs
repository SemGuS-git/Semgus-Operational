using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record RefVariableDeclaration  (IVariableInfo Inner)  : IVariableInfo, INode {
        public Identifier TypeId => Inner.TypeId;
        public Identifier Id => Inner.Id;

        public override string ToString() => "ref " + Inner.ToString();
    }
}
