using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class BitType : IType {
        public static BitType Instance { get; } = new();

        public Identifier Id { get; } = new("bit");

        public string Name => Id.Name;

        private BitType() { }

        public static Identifier AtomId { get; } = new("atom_bit");


        public static FunctionDefinition GetAtom() {
            var type = Instance;
            Variable var_a = new("a", type);
            Variable var_b = new("b", type);
            Variable var_t = new("t", IntType.Instance);

            return new(new FunctionSignature(AtomId, FunctionModifier.Generator, BitType.Instance, new[] { var_a, var_b }),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(X.L0, new ReturnStatement(var_a.Implies(var_b))),
                var_t.ElseIfEq(X.L1, new ReturnStatement(var_a.ImpliedBy(var_b))),
                new ReturnStatement(X.L1)
            );
        }

        public override string ToString() => Name;
    }
}
