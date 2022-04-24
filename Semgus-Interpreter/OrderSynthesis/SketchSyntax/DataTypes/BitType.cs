using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;
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

            return new(new FunctionSignature(FunctionModifier.Generator, Id, AtomId, new[] { var_a, var_b }),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(Lit0, Return(var_a.Implies(var_b))),
                var_t.IfEq(Lit1, Return(var_a.Implies(var_b))),
                new ReturnStatement(Lit1)
            );
        }

        public override string ToString() => Id.ToString();
    }
}
