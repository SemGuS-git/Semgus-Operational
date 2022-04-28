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
    internal static class StructTypeExtensions {
        public static StructDefinition GetStructDef(this StructType type) => new(type.Id, type.Elements) { Comment = type.Comment };

        public static FunctionDefinition GetEqualityFunction(this StructType type) {
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, type.EqId, var_a, var_b),
                new ReturnStatement(
                    new InfixOperation(Op.And,
                        type.Elements.Select(e => Op.Eq.Of(var_a.Get(e), var_b.Get(e))).ToList()
                    )
                )
            );
        }

        public static FunctionDefinition GetCompareGenerator(this StructType type) {
            var var_leq = Varn("leq", BitType.Id);
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, type.CompareId, var_a, var_b),
                new VariableDeclaration(var_leq, Lit0),
                new RepeatStatement(new Hole(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), type.DisjunctId.Call(var_a, var_b)))
                ),
                new ReturnStatement(var_leq.Ref())
            );
        }

        public static FunctionDefinition GetCompareRefinementGenerator(this StructType type, Identifier prevId, Variable budget) {
            var var_leq = Varn("leq", BitType.Id);
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, type.CompareId, var_a, var_b),
                var_leq.Declare(prevId.Call(var_a, var_b)),
                new RepeatStatement(budget.Ref(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), type.DisjunctId.Call(var_a, var_b)))
                ),
                Return(var_leq.Ref())
            );
        }

        public static FunctionDefinition GetCompareReductionGenerator(this StructType type, Variable budget) {
            var var_leq = Varn("leq", BitType.Id);
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, type.CompareId, var_a, var_b),
                var_leq.Declare(Lit0),
                new RepeatStatement(budget.Ref(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), type.DisjunctId.Call(var_a.Ref(), var_b.Ref())))
                ),
                Return(var_leq.Ref())
            );
        }


        public static FunctionDefinition GetDisjunctGenerator(this StructType type) {
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Generator, BitType.Id, type.DisjunctId, var_a, var_b),
                Return(
                    Op.And.Of(type.Elements.Select(e =>
                       CompareAtomGenerators.GetAtomFunctionId(e.TypeId).Call(var_a.Get(e), var_b.Get(e))
                    ).ToList())
                )
            );
        }


        public static FunctionDefinition GetNonEqualityHarness(this StructType type) {
            var var_a = Varn("a", type.Id);
            var var_b = Varn("b", type.Id);

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, type.NonEqId),
                var_a.Declare(type.NewFromHoles()),
                var_b.Declare(type.NewFromHoles()),
                Assertion(Not(type.EqId.Call(var_a, var_b))),
                Assertion(type.CompareId.Call(var_a, var_b))
            );
        }

        public static IEnumerable<IStatement> GetPartialEqAssertions(this StructType type, Variable a, Variable b, Variable c) {
            if (new[] { a, b, c }.Any(v => v.TypeId != type.Id)) throw new ArgumentException();

            yield return new Annotation($"{type.Id}: reflexivity and antisymmetry", 1);
            yield return new AssertStatement(
                Op.Eq.Of(
                    Op.And.Of(
                        type.CompareId.Call(a, b),
                        type.CompareId.Call(b, a)
                    ),
                    type.EqId.Call(a, b)
                )
            );

            yield return new Annotation($"{type.Id}: transitivity");
            yield return new AssertStatement(
                Op.Or.Of(
                    Not(type.CompareId.Call(a, b)),
                    Not(type.CompareId.Call(b, c)),
                    type.CompareId.Call(a, c)
                )
            );
        }
    }
}
