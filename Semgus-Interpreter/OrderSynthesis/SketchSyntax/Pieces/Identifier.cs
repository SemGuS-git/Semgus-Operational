namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Identifier(string Name) {
        public override string ToString() => Name;
    }
}
