using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;
    internal class CompareAtomGenerators {
        public static Identifier GetAtomFunctionId(Identifier typeId) => new($"atom_{typeId}");

        public static FunctionDefinition GetBitAtom() {
            var var_a = Varn("a", BitType.Id);
            var var_b = Varn("b", BitType.Id);
            var var_t = Varn("t", IntType.Id);

            return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, GetAtomFunctionId(BitType.Id), var_a, var_b),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(Lit0, Return(var_a.Implies(var_b))),
                var_t.IfEq(Lit1, Return(var_b.Implies(var_a))),
                new ReturnStatement(Lit1)
            );
        }

        public static FunctionDefinition GetIntAtom() {
            var var_a = Varn("a", IntType.Id);
            var var_b = Varn("b", IntType.Id);
            var var_t = Varn("t", IntType.Id);

            return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, GetAtomFunctionId(IntType.Id), var_a, var_b),
                new VariableDeclaration(var_t, new Hole()),
                var_t.IfEq(Lit0, Return(Op.Eq.Of(var_a, var_b))),
                var_t.IfEq(Lit1, Return(Op.Leq.Of(var_a, var_b))),
                var_t.IfEq(Lit2, Return(Op.Lt.Of(var_a, var_b))),
                var_t.IfEq(Lit3, Return(Op.Geq.Of(var_a, var_b))),
                var_t.IfEq(Lit4, Return(Op.Gt.Of(var_a, var_b))),
                new ReturnStatement(Lit1)
            );
        }
    }
}
