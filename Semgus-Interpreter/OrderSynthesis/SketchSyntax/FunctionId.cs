namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class FunctionId {
        public string Name { get; }

        public FunctionId(string name) {
            Name = name;
        }

        public override string ToString() => Name;
    }
}
