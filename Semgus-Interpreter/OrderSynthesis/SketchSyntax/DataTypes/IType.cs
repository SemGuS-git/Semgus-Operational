namespace Semgus.OrderSynthesis.SketchSyntax {
    internal interface IType {
        Identifier Id { get; }

        string Name => Id.Name;
    }
}
