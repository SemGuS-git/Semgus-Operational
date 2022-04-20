using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class TopOrBot : ILatticeSubstep {
            public bool IsTopElseBot { get; }
            public Identifier TargetId { get; }
            public StructType Subject { get; }
            public FunctionDefinition Compare { get; }
            static FunctionDefinition AtomBit { get; }
                = new(new FunctionSignature(FunctionModifier.Generator, BitType.Instance, new Identifier("fixed_atom_bit"), Array.Empty<Variable>()),
                    X.Return(new Hole())
                );

            static FunctionDefinition AtomInt { get; }
                = new Func<FunctionDefinition>(() => {
                    var v_t = new Variable("t", IntType.Instance);

                    return new(new FunctionSignature(FunctionModifier.Generator, IntType.Instance, new Identifier("fixed_atom_int"), Array.Empty<Variable>()),
                        v_t.Declare(new Hole()),
                        v_t.IfEq(X.L0, X.Return(-Shared.INT_MAX)),
                        v_t.IfEq(X.L1, X.Return(Shared.INT_MAX)),
                        X.Return(X.L0)
                    );
                }).Invoke();

            static FunctionDefinition GetAtom(IType type) => type switch {
                BitType => AtomBit,
                IntType => AtomInt,
                _ => throw new NotSupportedException(),
            };


            FunctionDefinition SynthesisTarget { get; }
            

            FunctionEval BoundCheck(FunctionDefinition bfn, Variable x) =>
                IsTopElseBot
                ? Compare.Call(x.Ref(), bfn.Call())
                : Compare.Call(bfn.Call(), x.Ref());

            public TopOrBot(bool isTopElseBot, StructType subject, FunctionDefinition compare) {
                this.IsTopElseBot = isTopElseBot;
                this.TargetId = new(isTopElseBot ? "top" : "bot");
                this.Subject = subject;
                this.Compare = compare;

                SynthesisTarget = new FunctionDefinition(new FunctionSignature(FunctionModifier.None, subject, TargetId, Array.Empty<Variable>()),
                    X.Return(
                        new StructNew(subject.Id, subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.Type).Call())).ToList())
                    )
                );
            }

            public IEnumerable<IStatement> GetInitialFile() {

                var fixed_atom_bit = AtomBit;
                var fixed_atom_int = AtomInt;

                FunctionDefinition next = SynthesisTarget;

                var a = new Variable("a", Subject);
                var test_bounds = new FunctionDefinition(new FunctionSignature(FunctionModifier.None, VoidType.Instance, new("test_bounds"), new[] { a }),
                    X.Assert(BoundCheck(next, a))
                );

                var main_bounds = Shared.GetMain(test_bounds, a);

                yield return Subject.GetStructDef();
                yield return Compare;
                yield return fixed_atom_bit;
                yield return fixed_atom_int;
                yield return next;
                yield return test_bounds;
                yield return main_bounds;
            }

            public IEnumerable<IStatement> GetRefinementFile(FunctionDefinition prev) {
                var fixed_atom_bit = AtomBit;
                var fixed_atom_int = AtomInt;

                FunctionDefinition next = SynthesisTarget;

                var a = new Variable("a", Subject);

                var test_bounds = new FunctionDefinition(new FunctionSignature(FunctionModifier.None, VoidType.Instance, new("test_bounds"), new[] { a }),
                    X.Assert(X.Not(BoundCheck(prev, a))),
                    X.Assert(BoundCheck(next, a))
                );

                var main_bounds = Shared.GetRefinementMain(test_bounds);

                yield return Subject.GetStructDef();
                yield return Compare;
                yield return fixed_atom_bit;
                yield return fixed_atom_int;
                yield return prev;
                yield return next;
                yield return test_bounds;
                yield return main_bounds;
            }

        }
    }
}
