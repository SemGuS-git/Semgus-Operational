namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class AnyType : IType {
        public static AnyType Instance { get; } = new();

        public static Identifier Id { get; } = new("any");
        Identifier IType.Id => Id;

        private AnyType() { }
        public override string ToString() => Id.ToString();
    }
}
