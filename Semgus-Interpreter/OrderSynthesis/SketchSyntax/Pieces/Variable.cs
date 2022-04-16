namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Variable (Identifier Id, IType Type)  : IVariableInfo  {
        public Identifier TypeId => Type.Id;

        public Variable(string name, IType type) : this(new Identifier(name), type) { }

        public override string ToString() => Id.ToString();
    }
}
