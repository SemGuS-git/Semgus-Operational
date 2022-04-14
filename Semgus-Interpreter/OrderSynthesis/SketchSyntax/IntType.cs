using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class IntType : IType {
        public static IntType Instance { get; } = new();

        public string Name => "int";
        private IntType() { }
        public static FunctionId AtomId { get; } = new("atom_int");
        public static FunctionDefinition GetAtom() {
            var type = Instance;
            VarId var_a = new("a", type);
            VarId var_b = new("b", type);
            VarId var_t = new("t", IntType.Instance);

            return new(AtomId, FunctionFlag.Generator, BitType.Instance, new[] { var_a, var_b },
                new VarDeclare(var_t, new Hole()),
                var_t.IfEq(X.L0, new ReturnStatement(X.Bi(var_a, Op.Eq,  var_b))),
                var_t.ElseIfEq(X.L1, new ReturnStatement(X.Bi(var_a, Op.Leq, var_b))),
                var_t.ElseIfEq(X.L2, new ReturnStatement(X.Bi(var_a, Op.Lt,  var_b))),
                var_t.ElseIfEq(X.L3, new ReturnStatement(X.Bi(var_a, Op.Geq, var_b))),
                var_t.ElseIfEq(X.L4, new ReturnStatement(X.Bi(var_a, Op.Gt,  var_b))),
                new ReturnStatement(new Literal(1))
            );
        }

        private static InfixOperation EqVal(IExpression lhs, int rhs) => new(Op.Eq, lhs, new Literal(rhs));

        public override string ToString() => Name;
    }
}
