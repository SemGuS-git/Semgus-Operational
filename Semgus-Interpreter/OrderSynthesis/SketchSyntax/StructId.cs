namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class StructId {
        public string Name { get; }

        public StructId(string name) {
            Name = name;
        }
        public override string ToString() => Name;
    }
}
