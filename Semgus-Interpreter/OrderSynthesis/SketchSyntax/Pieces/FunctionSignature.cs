using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record FunctionSignature(
        FunctionModifier Flag,
        Identifier ReturnTypeId,
        Identifier Id,
        IReadOnlyList<FunctionArg> Args,
        Identifier? ImplementsId
    ) : ISyntaxNode {
        public FunctionSignature(FunctionModifier flag, Identifier returnTypeId, Identifier id, IReadOnlyList<FunctionArg> args) : this(flag, returnTypeId, id, args, null) { }

        public FunctionSignature(FunctionModifier flag, Identifier returnTypeId, Identifier id, params FunctionArg[] args) : this(flag, returnTypeId, id, args, null) { }
        public FunctionSignature(Identifier returnTypeId, Identifier id, params FunctionArg[] args) : this(FunctionModifier.None, returnTypeId, id, args) { }

        public FunctionSignature(FunctionModifier flag, Identifier returnTypeId, Identifier id, Variable arg_var0, params Variable[] arg_vars) : this(flag, returnTypeId, id, ToArgs(arg_var0,arg_vars)) { }
        public FunctionSignature(Identifier returnTypeId, Identifier id, Variable arg_var0, params Variable[] arg_vars) : this(FunctionModifier.None, returnTypeId, id, ToArgs(arg_var0, arg_vars)) { }


        static IReadOnlyList<FunctionArg> ToArgs(Variable first, IEnumerable<Variable> rest) => rest.Prepend(first).Select(v => new FunctionArg(v.Id, v.TypeId)).ToList();

        public override string ToString() {
            var a = $"{GetPrefix(Flag)}{ReturnTypeId} {Id} ({string.Join(", ", Args.Select(a => a.IsRef ? $"ref {a.TypeId} {a.Id}" : $"{a.TypeId} {a.Id}"))})";
            if (ImplementsId is not null) a += $" implements {ImplementsId}";
            return a;
        }

        public static string GetPrefix(FunctionModifier flag) => flag switch {
            FunctionModifier.None => "",
            FunctionModifier.Harness => "harness ",
            FunctionModifier.Generator => "generator ",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public virtual bool Equals(FunctionSignature? other) => other is not null &&
            Flag.Equals(other.Flag) &&
            ReturnTypeId.Equals(other.ReturnTypeId) &&
            Id.Equals(other.Id) &&
            (ImplementsId is null && other.ImplementsId is null || (ImplementsId?.Equals(other.ImplementsId) ?? false)) &&
            Args.SequenceEqual(other.Args);

        public FunctionSignature AsFunctional(out Identifier? displacedRefVarId) {
            if (ReturnTypeId != VoidType.Id) {
                if (Args.Any(a => a.IsRef)) {
                    throw new InvalidOperationException();
                }
                displacedRefVarId = null;
                return this;
            }

            List<FunctionArg> pure_inputs = new();
            FunctionArg? the_ref_arg = null;

            foreach (var arg in Args) {
                if (arg.IsRef) {
                    if (the_ref_arg is not null) throw new InvalidOperationException();
                    the_ref_arg = arg;
                } else {
                    pure_inputs.Add(arg);
                }
            }

            if (the_ref_arg is not null) {
                displacedRefVarId = the_ref_arg.Id;
                return this with { ReturnTypeId = the_ref_arg.TypeId, Args = pure_inputs, ImplementsId = null };
            } else {
                displacedRefVarId = null;
                return this;
            }
        }
    }
}
