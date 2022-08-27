namespace Semgus.OrderSynthesis {
    internal record FlexPath (string Value) {
        public static FlexPath operator /(FlexPath a, string b) => new(Path.Combine(a.Value, b));

        public FlexPath Append(string more) => this / more;

        public override string ToString() => Value;
    }
}
