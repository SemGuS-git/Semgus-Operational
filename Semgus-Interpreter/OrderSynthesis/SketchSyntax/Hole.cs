namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class Hole : IExpression {
        public string? Label { get; }

        public Hole(string? label = null) {
            Label = label;
        }

        public override string ToString() => Label is null ? "??" : $"?? /*{Label}*/";
    }
}
