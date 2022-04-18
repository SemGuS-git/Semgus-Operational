namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class Empty : IExpression {
        public static Empty Instance { get; } = new Empty();

        private Empty() { }

        public override bool Equals(object? obj) => obj is Empty;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => string.Empty;
    }
}
