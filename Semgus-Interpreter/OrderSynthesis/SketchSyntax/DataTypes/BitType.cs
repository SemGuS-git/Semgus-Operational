using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class BitType : IType {
        public static BitType Instance { get; } = new();

        public static Identifier Id { get; } = new("bit");
        Identifier IType.Id => Id;

        public static Identifier AtomId { get; } = new("atom_bit");
        
        private BitType() { }

        public static FunctionDefinition GetAtom() {
            var type = Instance;
            Variable var_a = new("a", type);
            Variable var_b = new("b", type);
            Variable var_t = new("t", IntType.Instance);

            return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, AtomId, new[] { var_a, var_b }),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(X.L0, X.Return(var_a.Implies(var_b))),
                var_t.IfEq(X.L1, X.Return(var_a.Implies(var_b))),
                new ReturnStatement(X.L1)
            );
        }

        public override string ToString() => Id.ToString();
    }
}
