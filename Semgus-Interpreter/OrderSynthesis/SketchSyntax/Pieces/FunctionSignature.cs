using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record FunctionSignature(
        FunctionModifier Flag,
        Identifier ReturnTypeId,
        Identifier Id,
        IReadOnlyList<IVariableInfo> Args,
        Identifier? ImplementsId = null
    ) : INode {
        public FunctionSignature(FunctionModifier flag, IType returnType, Identifier id, IReadOnlyList<IVariableInfo> args) : this(flag, returnType.Id, id, args) { }
        public FunctionSignature(Identifier returnTypeId, Identifier id, IReadOnlyList<IVariableInfo> args) : this(FunctionModifier.None, returnTypeId, id, args) { }
        public FunctionSignature(IType returnType, Identifier id, IReadOnlyList<IVariableInfo> args) : this(FunctionModifier.None, returnType.Id, id, args) { }

        public override string ToString() {
            var a = $"{GetPrefix(Flag)}{ReturnTypeId} {Id} ({string.Join(", ", Args.Select(a => $"{a.TypeId} {a.Id}"))})";
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
            if (ReturnTypeId == VoidType.Id) {
                List<IVariableInfo> new_args = new();

                Identifier return_type_id = VoidType.Id;
                Identifier? return_var_id = null;

                bool found = false;


                foreach (var arg in Args) {
                    if (arg is RefVariableDeclaration r) {
                        if (!found) {
                            return_type_id = arg.TypeId;
                            return_var_id = arg.Id;
                            found = true;
                        } else {
                            throw new InvalidOperationException();
                        }
                    } else {
                        new_args.Add(arg);
                    }
                }

                if (found) {
                    displacedRefVarId = return_var_id;
                    return this with { ReturnTypeId = return_type_id, Args = new_args, ImplementsId = null };
                } else {
                    displacedRefVarId = null;
                    return this;
                }
            } else if (Args.Any(a => a is RefVariableDeclaration)) {
                throw new InvalidOperationException();
            } else {
                displacedRefVarId = null;
                return this;
            }
        }
    }
}
