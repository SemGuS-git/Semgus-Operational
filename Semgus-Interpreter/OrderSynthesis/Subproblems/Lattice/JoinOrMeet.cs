using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;

    namespace LatticeSubstep {
        internal class JoinOrMeet : ILatticeSubstep {
            static FunctionDefinition AtomBit { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = Varn("t", IntType.Instance);
                    var a = Varn("a", BitType.Instance);
                    var b = Varn("b", BitType.Instance);
                    var a_ref = a.Ref();
                    var b_ref = b.Ref();
                    return new(new FunctionSignature(FunctionModifier.Generator, BitType.Id, new Identifier("overlap_atom_bit"), a, b),
                        t.Declare(new Hole()),
                        t.IfEq(Lit0, Return(Op.And.Of(a_ref, b_ref))),
                        t.IfEq(Lit1, Return(Op.Or.Of(a_ref, b_ref))),
                        t.IfEq(Lit2, Return(Op.Eq.Of(a_ref, b_ref))),
                        t.IfEq(Lit3, Return(Op.Neq.Of(a_ref, b_ref))),
                        t.IfEq(Lit4, Return(UnaryOp.Not.Of(Op.Or.Of(a_ref, b_ref)))),
                        t.IfEq(Lit5, Return(Lit0)),
                        Return(Lit1)
                    );
                }).Invoke();

            static FunctionDefinition AtomInt { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = Varn("t", IntType.Instance);
                    var a = Varn("a", IntType.Instance);
                    var b = Varn("b", IntType.Instance);
                    var a_ref = a.Ref();
                    var b_ref = b.Ref();
                    return new(new FunctionSignature(FunctionModifier.Generator, IntType.Id, new Identifier("overlap_atom_int"), a, b),
                        t.Declare(new Hole()),
                        t.IfEq(Lit0, Return(new Ternary(Op.Lt.Of(a_ref, b_ref), a_ref, b_ref))),
                        t.IfEq(Lit1, Return(new Ternary(Op.Gt.Of(a_ref, b_ref), a_ref, b_ref))),
                        t.IfEq(Lit2, Return(Shared.IntMin)),
                        t.IfEq(Lit3, Return(Shared.IntMax)),
                        Return(Lit0)
                    );
                }).Invoke();


            static FunctionDefinition GetAtom(Identifier typeId)
                => typeId == BitType.Id ? AtomBit : typeId == IntType.Id ? AtomInt : throw new NotSupportedException();



            public bool IsJoinElseMeet { get; }
            private string Which { get; }
            public Identifier SynthFunId => SynthesisTarget.Id;
            public StructType Subject { get; }

            public FunctionDefinition Eq { get; }
            public FunctionDefinition Compare { get; }

            private FunctionDefinition SynthesisTarget { get; }

            public JoinOrMeet(bool isJoinElseMeet, StructType subject, FunctionDefinition compare) {
                this.IsJoinElseMeet = isJoinElseMeet;
                this.Which = IsJoinElseMeet ? "join" : "meet";
                this.Subject = subject;
                this.Eq = subject.GetEqualityFunction();
                this.Compare = compare;
                this.SynthesisTarget = MakeSynthesisTarget(new($"{subject.Id}_{Which}"), subject);

            }
            static FunctionDefinition MakeSynthesisTarget(Identifier id, StructType subject) {
                var a = Varn("a", subject);
                var b = Varn("b", subject);
                return new FunctionDefinition(new FunctionSignature(subject.Id, id, a, b),
                    Return(
                        new StructNew(subject.Id, subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.TypeId).Call(a.Get(e), b.Get(e)))).ToList())
                    )
                );
            }

            public IEnumerable<IStatement> GetInitialFile() {

                var a = Varn("a", Subject);
                var b = Varn("b", Subject);

                var op_val = Varn($"{Which}_ab", Subject);

                FunctionEval BoundCheck(Variable x) =>
                    IsJoinElseMeet
                    ? Compare.Call(x, op_val)  // x <= Join
                    : Compare.Call(op_val, x); // Meet <= x


                var test_correct = new FunctionDefinition(new FunctionSignature(VoidType.Id, new($"test_{Which}"), a, b),
                    op_val.Declare(SynthesisTarget.Call(a, b)),
                    If(Compare.Call(a, b),
                        Assertion(
                            Eq.Call(op_val, IsJoinElseMeet ? b : a) // if a <= b: Join(a,b) = b, Meet(a,b) = a
                        )
                    ).ElseIf(Compare.Call(b, a),
                        Assertion(
                            Eq.Call(op_val, IsJoinElseMeet ? a : b) // if b <= a: Join(a,b) = a, Meet(a,b) = b
                        )
                    ).Else(
                        Assertion(Op.And.Of(BoundCheck(a), BoundCheck(b))) // if incomparable: each(a,b) <= Join(a,b), Meet(a,b) <= each(a,b)
                    )
                );

                var forall_test_correct = Shared.GetForallTestHarness(Subject, test_correct, a, b);

                yield return Subject.GetStructDef();
                yield return Eq;
                yield return Compare;
                yield return AtomBit;
                yield return AtomInt;
                yield return SynthesisTarget;
                yield return test_correct;
                yield return forall_test_correct;
            }

            public IEnumerable<IStatement> GetRefinementFile(FunctionDefinition prev) {
                foreach (var st in GetInitialFile()) yield return st;
                yield return prev;
                yield return GetImproveHarness(prev);
            }

            FunctionDefinition GetImproveHarness(FunctionDefinition prev_def) {
                var a = Varn("a", Subject);
                var b = Varn("b", Subject);

                IExpression ab_incomparable = Not(Op.Or.Of(Compare.Call(a, b), Compare.Call(b, a)));

                var prev_val = Varn($"prev_{Which}_ab", Subject);
                var next_val = Varn($"next_{Which}_ab", Subject);

                IExpression is_tighter_bound = IsJoinElseMeet
                    // next_join < prev_join
                    ? Op.And.Of(
                        Compare.Call(next_val, prev_val),
                        Not(Compare.Call(prev_val, next_val))
                    )
                    // next_meet < prev_meet
                    : (IExpression)Op.And.Of(
                        Compare.Call(prev_val, next_val),
                        Not(Compare.Call(next_val, prev_val))
                    );

                return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"improve_{Which}")),
                    a.Declare(Subject.NewFromHoles()),
                    b.Declare(Subject.NewFromHoles()),
                    Assertion(ab_incomparable),
                    prev_val.Declare(prev_def.Call(a, b)),
                    next_val.Declare(SynthesisTarget.Call(a, b)),
                    Assertion(is_tighter_bound)
                );
            }
        }
    }
}
