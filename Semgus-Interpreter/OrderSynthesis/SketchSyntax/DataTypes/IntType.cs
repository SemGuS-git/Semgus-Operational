using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;
    internal class IntType : IType {
        public static IntType Instance { get; } = new();

        public static Identifier Id { get; } = new("int");
        Identifier IType.Id => Id;

        private IntType() { }

        public override string ToString() => Id.ToString();
    }
}
