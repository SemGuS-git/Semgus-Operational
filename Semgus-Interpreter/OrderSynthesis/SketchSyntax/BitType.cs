using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class BitType : IType {
        public static BitType Instance { get; } = new();

        public string Name => "bit";

        private BitType() { }

        public static FunctionId AtomId { get; } = new("atom_bit");
        public static FunctionDefinition GetAtom() {
            var type = Instance;
            VarId var_a = new("a", type);
            VarId var_b = new("b", type);
            VarId var_t = new("t", IntType.Instance);

            return new(AtomId, FunctionFlag.Generator, BitType.Instance, new[] { var_a, var_b },
                new VarDeclare(var_t, new Hole()),
                var_t.IfEq(X.L0, new ReturnStatement(var_a.Implies(var_b))),
                var_t.ElseIfEq(X.L1, new ReturnStatement(var_a.ImpliedBy(var_b))),
                new ReturnStatement(X.L1)
            );
        }

        public override string ToString() => Name;
    }
}
