using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class IntType : IType {
        public static IntType Instance { get; } = new();

        public static Identifier Id { get; } = new("int");
        Identifier IType.Id => Id;

        public static Identifier AtomId { get; } = new("atom_int");

        private IntType() { }

        public static FunctionDefinition GetAtom() {
            var type = Instance;
            Variable var_a = new("a", type);
            Variable var_b = new("b", type);
            Variable var_t = new("t", IntType.Instance);

            return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, AtomId,new[] { var_a, var_b }),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(X.L0, X.Return(Op.Eq .Of(var_a, var_b))),
                var_t.IfEq(X.L1, X.Return(Op.Leq.Of(var_a, var_b))),
                var_t.IfEq(X.L2, X.Return(Op.Lt .Of(var_a, var_b))),
                var_t.IfEq(X.L3, X.Return(Op.Geq.Of(var_a, var_b))),
                var_t.IfEq(X.L4, X.Return(Op.Gt .Of(var_a, var_b))),
                new ReturnStatement(new Literal(1))
            );
        }

        private static InfixOperation EqVal(IExpression lhs, int rhs) => new(Op.Eq, lhs, new Literal(rhs));

        public override string ToString() => Id.ToString();
    }
}
