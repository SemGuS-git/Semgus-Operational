namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record FunctionEval  (Identifier Id, IReadOnlyList<IExpression> Args)  : IExpression, IStatement  {
        public FunctionEval(Identifier id, params IExpression[] args) : this(id, args.ToList()) { }

        public override string ToString() => $"{Id}({string.Join(", ", Args)})";


        public virtual bool Equals(FunctionEval? other) => other is not null && Id.Equals(other.Id) && Args.SequenceEqual(other.Args);

        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString() + ";");
    }
}
