using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class TopOrBot : ILatticeSubstep {
            public bool IsTopElseBot { get; }
            private string Which { get; }
            public Identifier SynthFunId { get; }
            public StructType Subject { get; }
            public FunctionDefinition Compare { get; }
            private FunctionDefinition SynthGenerator { get; }

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


            public TopOrBot(bool isTopElseBot, StructType subject, FunctionDefinition compare) {
                this.IsTopElseBot = isTopElseBot;
                this.Which = isTopElseBot ? "top" : "bot";
                this.SynthFunId = new($"{subject.Id.Name}_{Which}");
                this.Subject = subject;
                this.Compare = compare;

                SynthGenerator = new FunctionDefinition(new FunctionSignature(subject, SynthFunId, Array.Empty<Variable>()),
                    X.Return(
                        new StructNew(subject.Id, subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.Type).Call())).ToList())
                    )
                );
            }

            public IEnumerable<IStatement> GetInitialFile() {
                var a = new Variable("a", Subject);

                var check_bound = IsTopElseBot
                    ? Compare.Call(a.Ref(), SynthGenerator.Call())  // variable <= TOP
                    : Compare.Call(SynthGenerator.Call(), a.Ref()); // BOT <= variable


                var assert_bound_holds = new FunctionDefinition(new FunctionSignature(VoidType.Instance, new($"test_{Which}"), new[] { a }),
                    X.Assert(check_bound)
                );

                var bound_holds_forall = Shared.GetForallTestHarness(assert_bound_holds, a);

                yield return Subject.GetStructDef();
                yield return Compare;
                yield return AtomBit;
                yield return AtomInt;
                yield return SynthGenerator;
                yield return assert_bound_holds;
                yield return bound_holds_forall;
            }

            public IEnumerable<IStatement> GetRefinementFile(FunctionDefinition prev) {
                foreach (var st in GetInitialFile()) {
                    yield return st;
                }
                yield return prev;
                yield return GetImproveHarness(prev);
            }

            FunctionDefinition GetImproveHarness(FunctionDefinition prev_def) {
                Variable prev_val = new($"prev_{Which}", Subject);
                Variable next_val = new($"next_{Which}", Subject);

                IExpression is_tighter_bound = IsTopElseBot
                    // next_top < prev_top
                    ? Op.And.Of(
                        Compare.Call(next_val, prev_val),
                        X.Not(Compare.Call(prev_val, next_val))
                    )
                    // next_bot < prev_bot
                    : (IExpression)Op.And.Of(
                        Compare.Call(prev_val, next_val),
                        X.Not(Compare.Call(next_val, prev_val))
                    );

                return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Instance, new($"improve_{Which}"), Array.Empty<IVariableInfo>()),
                    prev_val.Declare(prev_def.Call()),
                    next_val.Declare(SynthGenerator.Call()),
                    X.Assert(is_tighter_bound)
                );
            }
        }
    }
}
