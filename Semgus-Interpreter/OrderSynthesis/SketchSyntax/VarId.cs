namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class VarId : IExpression, ISettable {
        public string Name { get; }
        public IType Type { get; }

        public VarId(string name, IType type) {
            Name = name;
            Type = type;
        }

        public string GetArgString() => $"{Type} {Name}";

        public override string ToString() => Name;
    }
}
