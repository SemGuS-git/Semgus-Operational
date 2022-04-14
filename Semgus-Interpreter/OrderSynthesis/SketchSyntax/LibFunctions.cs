namespace Semgus.OrderSynthesis.SketchSyntax {
    internal static class LibFunctions {
        public static FunctionId Not { get; } = new("!");
        public static FunctionId? MapSmtOrNull(string name) => name switch {
            "not" => Not,
            _ => null,
        };

    }
}
