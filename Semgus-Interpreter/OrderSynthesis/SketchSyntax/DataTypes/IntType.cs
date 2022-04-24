using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;
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
            Variable var_t = new("t", Instance);

            return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, AtomId,new[] { var_a, var_b }),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(Lit0, Return(Op.Eq .Of(var_a, var_b))),
                var_t.IfEq(Lit1, Return(Op.Leq.Of(var_a, var_b))),
                var_t.IfEq(Lit2, Return(Op.Lt .Of(var_a, var_b))),
                var_t.IfEq(Lit3, Return(Op.Geq.Of(var_a, var_b))),
                var_t.IfEq(Lit4, Return(Op.Gt .Of(var_a, var_b))),
                new ReturnStatement(new Literal(1))
            );
        }

        public override string ToString() => Id.ToString();
    }
}
