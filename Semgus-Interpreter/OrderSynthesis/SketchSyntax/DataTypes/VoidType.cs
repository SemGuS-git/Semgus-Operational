namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class VoidType : IType {
        public static VoidType Instance { get; } = new();

        public Identifier Id { get; } = new("void");
        public string Name => Id.Name;

        private VoidType() { }
        public override string ToString() => Name;
    }
}
