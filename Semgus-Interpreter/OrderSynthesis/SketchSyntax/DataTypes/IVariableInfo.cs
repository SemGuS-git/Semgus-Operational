using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal interface IVariableInfo {
        Identifier TypeId { get; }
        Identifier Id { get; }
    }
}
