namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Hole  (string? Label = null)  : IExpression  {
        public override string ToString() => Label is null ? "??" : $"?? /*{Label}*/";
    }
}
