namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class VoidType : IType {
        public static VoidType Instance { get; } = new();

        public string Name => "void";

        private VoidType() { }
        public override string ToString() => Name;
    }
}
