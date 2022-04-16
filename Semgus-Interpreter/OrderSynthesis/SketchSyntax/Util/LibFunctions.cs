namespace Semgus.OrderSynthesis.SketchSyntax {
    internal static class LibFunctions {
        public static Identifier Not { get; } = new("!");
        public static Identifier? MapSmtOrNull(string name) => name switch {
            "not" => Not,
            _ => null,
        };
    }
}
