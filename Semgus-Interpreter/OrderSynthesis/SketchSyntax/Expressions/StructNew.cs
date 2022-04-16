namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record StructNew(Identifier Type, IReadOnlyList<Assignment> Args) : IExpression {
        public StructNew(Identifier type, params Assignment[] args) : this(type, args.ToList()) { }

        public override string ToString() => $"new {Type}({string.Join(", ", Args)})";

        public virtual bool Equals(StructNew? other) => other is not null && Type.Equals(other.Type) && Args.SequenceEqual(other.Args);
    }
}
