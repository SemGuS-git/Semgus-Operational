using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    internal record Identifier(string Name) : IToken, INode {
        public override string ToString() => $"{Name}";
    }
}
