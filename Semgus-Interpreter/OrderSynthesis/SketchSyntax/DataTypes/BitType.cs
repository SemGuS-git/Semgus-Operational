using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;
    internal class BitType : IType {
        public static BitType Instance { get; } = new();

        public static Identifier Id { get; } = new("bit");
        Identifier IType.Id => Id;
        
        private BitType() { }


        public override string ToString() => Id.ToString();
    }
}
