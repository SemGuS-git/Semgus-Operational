namespace Semgus.OrderSynthesis.SketchSyntax.Parsing.Iota {
    internal class InvalidTokenException : Exception {
        public string BadString { get; }

        public InvalidTokenException(string bad) : base($"Error parsing \"{bad}\"") {
            BadString = bad;
        }
        public InvalidTokenException(string bad, string explain) : base($"Error parsing \"{bad}\": {explain}") {
            BadString = bad;
        }
    }
}
