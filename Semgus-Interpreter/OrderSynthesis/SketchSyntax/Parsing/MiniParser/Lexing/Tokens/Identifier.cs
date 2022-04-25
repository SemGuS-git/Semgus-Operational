using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    internal record Identifier(string Name) : IToken, ISyntaxNode {
        public override string ToString() => $"{Name}";
    }
}
