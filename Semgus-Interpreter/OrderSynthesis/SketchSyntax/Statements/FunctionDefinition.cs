namespace Semgus.OrderSynthesis.SketchSyntax {
    internal interface IFunctionSignature {
        FunctionModifier Flag { get; }
        Identifier ReturnTypeId { get; }
        Identifier Id { get; }

        Identifier? ImplementsId { get; }

        IReadOnlyList<IVariableInfo> Args { get; }
    }
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
    }

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
    }

    internal record FunctionDefinition (IFunctionSignature Signature, IReadOnlyList<IStatement> Body)  : IStatement  {
        public string? Alias { get; set; } = null;
        public Identifier Id => Signature.Id;

        public FunctionDefinition(IFunctionSignature signature, params IStatement[] body) : this(signature, body.ToList()) { }


        public void WriteInto(ILineReceiver lineReceiver) {
            if (Alias is not null) lineReceiver.Add($"// {Alias}");
            lineReceiver.Add($"{Signature} {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
            lineReceiver.Add(""); // blank line
        }

        public virtual bool Equals(FunctionDefinition? other) =>
            other is not null &&
            Signature.Equals(other.Signature) &&
            EqualityComparer<string>.Default.Equals(Alias,other.Alias) &&
            Body.SequenceEqual(other.Body);

        public override string ToString() => this.PrettyPrint(true);
    }
}
