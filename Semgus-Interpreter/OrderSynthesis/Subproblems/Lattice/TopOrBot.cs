using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;

    namespace LatticeSubstep {
        internal class TopOrBot : ILatticeSubstep {
            public bool IsTopElseBot { get; }
            private string Which { get; }
            public Identifier SynthFunId { get; }
            public StructType Subject { get; }
            public FunctionDefinition Compare { get; }
            private FunctionDefinition SynthGenerator { get; }

            static FunctionDefinition AtomBit { get; }
                = new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, new Identifier("fixed_atom_bit")),
                    Return(new Hole())
                );

            static FunctionDefinition AtomInt { get; }
                = new Func<FunctionDefinition>(() => {
                    var v_t = new Variable("t", IntType.Instance);

                    return new(new FunctionSignature(FunctionModifier.Generator, IntType.Id, new Identifier("fixed_atom_int")),
                        v_t.Declare(new Hole()),
                        v_t.IfEq(Lit0, Return(Shared.IntMin)),
                        v_t.IfEq(Lit1, Return(Shared.IntMax)),
                        Return(Lit0)
                    );
                }).Invoke();

            static FunctionDefinition GetAtom(Identifier typeId)
                => typeId == BitType.Id ? AtomBit : typeId == IntType.Id ? AtomInt : throw new NotSupportedException();


            public TopOrBot(bool isTopElseBot, StructType subject, FunctionDefinition compare) {
                this.IsTopElseBot = isTopElseBot;
                this.Which = isTopElseBot ? "top" : "bot";
                this.SynthFunId = new($"{subject.Id.Name}_{Which}");
                this.Subject = subject;
                this.Compare = compare;

                SynthGenerator = new FunctionDefinition(new FunctionSignature(subject.Id, SynthFunId),
                    Return(
                        new StructNew(subject.Id, subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.TypeId).Call())).ToList())
                    )
                );
            }

            public IEnumerable<IStatement> GetInitialFile() {
                var a = new Variable("a", Subject);

                var check_bound = IsTopElseBot
                    ? Compare.Call(a.Ref(), SynthGenerator.Call())  // variable <= TOP
                    : Compare.Call(SynthGenerator.Call(), a.Ref()); // BOT <= variable


                var assert_bound_holds = new FunctionDefinition(new FunctionSignature(VoidType.Id, new($"test_{Which}"), a),
                    Assertion(check_bound)
                );

                var bound_holds_forall = Shared.GetForallTestHarness(Subject, assert_bound_holds, a);

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
                        Not(Compare.Call(prev_val, next_val))
                    )
                    // next_bot < prev_bot
                    : (IExpression)Op.And.Of(
                        Compare.Call(prev_val, next_val),
                        Not(Compare.Call(next_val, prev_val))
                    );

                return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"improve_{Which}")),
                    prev_val.Declare(prev_def.Call()),
                    next_val.Declare(SynthGenerator.Call()),
                    Assertion(is_tighter_bound)
                );
            }
        }
    }
}
