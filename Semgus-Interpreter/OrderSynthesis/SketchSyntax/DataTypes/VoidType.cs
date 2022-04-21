using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class VoidType : IType {
        public static VoidType Instance { get; } = new();

        public static Identifier Id { get; } = new("void");
        Identifier IType.Id => Id;

        private VoidType() { }
        public override string ToString() => Id.ToString();
    }
}
