namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record FunctionSignature  (FunctionModifier Flag, IType ReturnType, Identifier Id,IReadOnlyList<Variable> Args)  : IFunctionSignature  {

        public Identifier ReturnTypeId => ReturnType.Id;

        public Identifier? ImplementsId { get; init; } = null;

        IReadOnlyList<IVariableInfo> IFunctionSignature.Args => Args;

        public FunctionSignature(Identifier id, FunctionModifier flag, IType returnType, IReadOnlyList<Variable> args) : this(flag, returnType, id, args) { }


        public static string GetPrefix(FunctionModifier flag) => flag switch {
            FunctionModifier.None => "",
            FunctionModifier.Harness => "harness ",
            FunctionModifier.Generator => "generator ",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public override string ToString() => $"{GetPrefix(Flag)}{ReturnTypeId} {Id} ({string.Join(", ", Args.Select(a => $"{a.TypeId} {a.Id}"))})";



        public FunctionSignature AsHydrated(IReadOnlyDictionary<Identifier, IType> typeDict, Identifier? replacement_id = null) => replacement_id is null ? this : this with { Id = replacement_id };

        public IFunctionSignature AsFunctional() => this;
    }
}
