using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class JoinOrMeet : ILatticeSubstep {
            static FunctionDefinition AtomBit { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = new Variable("t", IntType.Instance);
                    var a = new VariableRef(new("a"));
                    var b = new VariableRef(new("b"));
                    return new(new FunctionSignature(FunctionModifier.Generator, BitType.Instance, new Identifier("overlap_atom_bit"), Array.Empty<Variable>()),
                        t.Declare(new Hole()),
                        t.IfEq(X.L0, X.Return(Op.And.Of(a, b))),
                        t.IfEq(X.L1, X.Return(Op.Or.Of(a, b))),
                        t.IfEq(X.L2, X.Return(Op.Eq.Of(a, b))),
                        t.IfEq(X.L3, X.Return(Op.Neq.Of(a, b))),
                        t.IfEq(X.L4, X.Return(UnaryOp.Not.Of(Op.Or.Of(a, b)))),
                        t.IfEq(X.L5, X.Return(X.L0)),
                        X.Return(X.L1)
                    );
                }).Invoke();

            static FunctionDefinition AtomInt { get; }
                = new Func<FunctionDefinition>(() => {
                    var t = new Variable("t", IntType.Instance);
                    var a = new VariableRef(new("a"));
                    var b = new VariableRef(new("b"));
                    return new(new FunctionSignature(FunctionModifier.Generator, IntType.Instance, new Identifier("overlap_atom_int"), Array.Empty<Variable>()),
                        t.Declare(new Hole()),
                        t.IfEq(X.L0, X.Return(new Ternary(Op.Lt.Of(a, b), a, b))),
                        t.IfEq(X.L1, X.Return(new Ternary(Op.Gt.Of(a, b), a, b))),
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
            public Identifier TargetId { get; }
            public StructType Subject { get; }
            public FunctionDefinition Compare { get; }

            public JoinOrMeet(bool isJoinElseMeet, StructType subject, FunctionDefinition compare) {
                this.IsJoinElseMeet = isJoinElseMeet;
                this.TargetId = new(isJoinElseMeet ? "join" : "meet");
                this.Subject = subject;
                this.Compare = compare;
            }


            FunctionDefinition SynthesisTarget(Variable a, Variable b) {
                return new FunctionDefinition(new FunctionSignature(FunctionModifier.None, Subject, TargetId, new[] { a, b }),
                    X.Return(
                        new StructNew(Subject.Id, Subject.Elements.Select(e => new VariableRef(e.Id).Assign(GetAtom(e.Type).Call(a.Get(e), b.Get(e)))).ToList())
                    )
                );
            }

            FunctionEval BoundCheck(Variable bound, Variable x) =>
                IsJoinElseMeet
                ? Compare.Call(x.Ref(), bound.Ref())
                : Compare.Call(bound.Ref(), x.Ref());


            public IEnumerable<IStatement> GetInitialFile() {

                var a = new Variable("a", Subject);
                var b = new Variable("b", Subject);

                var dyn_atom_bit = AtomBit;
                var dyn_atom_int = AtomInt;

                var next = SynthesisTarget(a, b);

                var v_val = new Variable(next.Id.Name + "_value", Subject);

                var test_overlap = new FunctionDefinition(new FunctionSignature(FunctionModifier.None, VoidType.Instance, new("test_overlap"), new[] { a, b }),
                    v_val.Declare(next.Call(a.Ref(), b.Ref())),
                    X.Assert(BoundCheck(v_val, a)),
                    X.Assert(BoundCheck(v_val, b))
                );

                var main_overlap = Shared.GetMain(test_overlap, a, b);

                yield return Subject.GetStructDef();
                yield return Compare;
                yield return dyn_atom_bit;
                yield return dyn_atom_int;
                yield return next;
                yield return test_overlap;
                yield return main_overlap;
            }

            public IEnumerable<IStatement> GetRefinementFile(FunctionDefinition prev) {

                var a = new Variable("a", Subject);
                var b = new Variable("b", Subject);

                var dyn_atom_bit = AtomBit;
                var dyn_atom_int = AtomInt;

                var next = SynthesisTarget(a, b);

                var prev_val = new Variable(prev.Id.Name + "_value", Subject);
                var next_val = new Variable(next.Id.Name + "_value", Subject);

                var test_overlap = new FunctionDefinition(new FunctionSignature(FunctionModifier.None, VoidType.Instance, new("test_overlap"), new[] { a, b }),
                    prev_val.Declare(prev.Call(a.Ref(), b.Ref())),
                    next_val.Declare(next.Call(a.Ref(), b.Ref())),
                    X.Assert(Op.Or.Of(
                        X.Not(BoundCheck(prev_val, a)),
                        X.Not(BoundCheck(prev_val, b))
                    )),
                    X.Assert(BoundCheck(next_val, a)),
                    X.Assert(BoundCheck(next_val, b))
                );

                var main_overlap = Shared.GetMain(test_overlap, a, b);

                yield return Subject.GetStructDef();
                yield return Compare;
                yield return dyn_atom_bit;
                yield return dyn_atom_int;
                yield return prev;
                yield return next;
                yield return test_overlap;
                yield return main_overlap;
            }

        }
    }
}
