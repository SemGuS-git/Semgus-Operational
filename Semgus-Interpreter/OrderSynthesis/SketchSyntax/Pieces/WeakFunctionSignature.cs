namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record WeakFunctionSignature(FunctionModifier Flag, Identifier ReturnTypeId, Identifier Id, IReadOnlyList<IVariableInfo> Args) : IFunctionSignature {
        public Identifier? ImplementsId { get; init; } = null;

        public override string ToString() => $"{FunctionSignature.GetPrefix(Flag)}{ReturnTypeId} {Id} ({string.Join(", ", Args.Select(a => $"{a.TypeId} {a.Id}"))})";

        public virtual bool Equals(WeakFunctionSignature? other) =>
            other is not null &&
            Flag.Equals(other.Flag) &&
            ReturnTypeId.Equals(other.ReturnTypeId) &&
            Id.Equals(other.Id) &&
            EqualityComparer<Identifier>.Default.Equals(ImplementsId, other.ImplementsId) &&
            Args.SequenceEqual(other.Args);


        public IFunctionSignature AsFunctional() {
            if (ReturnTypeId == VoidType.Id) {
                List<IVariableInfo> new_args = new();
                Identifier return_type_id = VoidType.Id;
                foreach(var arg in Args) {
                    if(arg is RefVariableDeclaration r) {
                        if(return_type_id == VoidType.Id) {
                            return_type_id = arg.TypeId;
                        } else {
                            throw new InvalidOperationException();
                        }
                    } else {
                        new_args.Add(arg);
                    }
                }
                if (return_type_id == VoidType.Id) {
                    return this;
                } else {
                    return this with { ReturnTypeId = return_type_id, Args = new_args, ImplementsId = this.ImplementsId };
                }
            } else if (Args.Any(a => a is RefVariableDeclaration)) {
                 throw new InvalidOperationException();
            } else {
                return this;
            }
        }
        public FunctionSignature AsHydrated(IReadOnlyDictionary<Identifier, IType> typeDict, Identifier? replacement_id = null) 
            => new(
                Flag,
                typeDict[ReturnTypeId],
                replacement_id ?? Id,
                Args.Select(
                    a => a is RefVariableDeclaration ?
                    throw new InvalidOperationException() :
                    new Variable(a.Id, typeDict[a.TypeId])
                ).ToList()
            ) { ImplementsId = this.ImplementsId };
    }
}
