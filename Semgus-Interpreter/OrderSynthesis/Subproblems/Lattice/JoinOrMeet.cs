using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class JoinOrMeet : ILatticeSubstep {
            static FunctionDefinition AtomBit { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = new Variable("t", IntType.Instance);
                    var a = new Variable("a", BitType.Instance);
                    var b = new Variable("b", BitType.Instance);
                    var a_ref = a.Ref();
                    var b_ref = b.Ref();
                    return new(new FunctionSignature(FunctionModifier.Generator, BitType.Instance, new Identifier("overlap_atom_bit"), new[] { a, b }),
                        t.Declare(new Hole()),
                        t.IfEq(X.L0, X.Return(Op.And.Of(a_ref, b_ref))),
                        t.IfEq(X.L1, X.Return(Op.Or.Of(a_ref, b_ref))),
                        t.IfEq(X.L2, X.Return(Op.Eq.Of(a_ref, b_ref))),
                        t.IfEq(X.L3, X.Return(Op.Neq.Of(a_ref, b_ref))),
                        t.IfEq(X.L4, X.Return(UnaryOp.Not.Of(Op.Or.Of(a_ref, b_ref)))),
                        t.IfEq(X.L5, X.Return(X.L0)),
                        X.Return(X.L1)
                    );
                }).Invoke();

            static FunctionDefinition AtomInt { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = new Variable("t", IntType.Instance);
                    var a = new Variable("a", IntType.Instance);
                    var b = new Variable("b", IntType.Instance);
                    var a_ref = a.Ref();
                    var b_ref = b.Ref();
                    return new(new FunctionSignature(FunctionModifier.Generator, IntType.Instance, new Identifier("overlap_atom_int"), new[] { a, b }),
                        t.Declare(new Hole()),
                        t.IfEq(X.L0, X.Return(new Ternary(Op.Lt.Of(a_ref, b_ref), a_ref, b_ref))),
                        t.IfEq(X.L1, X.Return(new Ternary(Op.Gt.Of(a_ref, b_ref), a_ref, b_ref))),
                        t.IfEq(X.L2, X.Return(-Shared.INT_MAX)),
                        t.IfEq(X.L3, X.Return(Shared.INT_MAX)),
                        X.Return(X.L0)
                    );
                }).Invoke();


            static FunctionDefinition GetAtom(IType type) => type switch {
                BitType => AtomBit,
                IntType => AtomInt,
                _ => throw new NotSupportedException(),
            };



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
                var a = new Variable("a", subject);
                var b = new Variable("b", subject);
                return new FunctionDefinition(new FunctionSignature(subject,id, new[] { a, b }),
                    X.Return(
                        new StructNew(subject.Id, subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.Type).Call(a.Get(e), b.Get(e)))).ToList())
                    )
                );
            }

            public IEnumerable<IStatement> GetInitialFile() {

                var a = new Variable("a", Subject);
                var b = new Variable("b", Subject);

                var op_val = new Variable($"{Which}_ab", Subject);

                FunctionEval BoundCheck(Variable x) =>
                    IsJoinElseMeet
                    ? Compare.Call(x, op_val)  // x <= Join
                    : Compare.Call(op_val, x); // Meet <= x


                var test_correct = new FunctionDefinition(new FunctionSignature(VoidType.Instance, new($"test_{Which}"), new[] { a, b }),
                    op_val.Declare(SynthesisTarget.Call(a,b)),
                    X.If(Compare.Call(a,b),
                        X.Assert(
                            Eq.Call(op_val, IsJoinElseMeet ? b : a) // if a <= b: Join(a,b) = b, Meet(a,b) = a
                        )
                    ).ElseIf(Compare.Call(b,a),
                        X.Assert(
                            Eq.Call(op_val, IsJoinElseMeet ? a : b) // if b <= a: Join(a,b) = a, Meet(a,b) = b
                        )
                    ).Else(
                        X.Assert(Op.And.Of(BoundCheck(a),BoundCheck(b))) // if incomparable: each(a,b) <= Join(a,b), Meet(a,b) <= each(a,b)
                    )
                );

                var forall_test_correct = Shared.GetForallTestHarness(test_correct, a, b);

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
                Variable a = new("a", Subject);
                Variable b = new("b", Subject);

                IExpression ab_incomparable = X.Not(Op.Or.Of(Compare.Call(a, b), Compare.Call(b, a)));

                Variable prev_val = new($"prev_{Which}_ab", Subject);
                Variable next_val = new($"next_{Which}_ab", Subject);

                IExpression is_tighter_bound = IsJoinElseMeet
                    // next_join < prev_join
                    ? Op.And.Of(
                        Compare.Call(next_val, prev_val),
                        X.Not(Compare.Call(prev_val, next_val))
                    )
                    // next_meet < prev_meet
                    : (IExpression)Op.And.Of(
                        Compare.Call(prev_val, next_val),
                        X.Not(Compare.Call(next_val, prev_val))
                    );

                return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Instance, new($"improve_{Which}"), Array.Empty<IVariableInfo>()),
                    a.Declare(Subject.NewFromHoles()),
                    b.Declare(Subject.NewFromHoles()),
                    X.Assert(ab_incomparable),
                    prev_val.Declare(prev_def.Call(a,b)),
                    next_val.Declare(SynthesisTarget.Call(a,b)),
                    X.Assert(is_tighter_bound)
                );
            }
        }
    }
}
