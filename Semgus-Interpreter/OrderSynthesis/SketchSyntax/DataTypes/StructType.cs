using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;

    internal class StructType : IType {
        public Identifier Id { get; }
        public string Name => Id.Name;

        public string? Comment { get; set; }

        public IReadOnlyList<Variable> Elements { get; }

        public Identifier CompareId { get; }
        public Identifier DisjunctId { get; }
        public Identifier EqId { get; }
        public Identifier NonEqId { get; }
        public Identifier BotTopValues { get; }

        public StructType(Identifier id, IReadOnlyList<Variable> elements) {
            this.Id = id;
            this.Elements = elements;

            CompareId = new($"compare_{id}");
            EqId = new($"eq_{id}");
            DisjunctId = new($"disjunct_{id}");
            NonEqId = new($"non_eq_{id}");
        }

        public override string ToString() => Id.ToString();

        public StructDefinition GetStructDef() => new(Id, Elements) { Comment = Comment };

        public FunctionDefinition GetEqualityFunction() {
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, EqId, new[] { var_a, var_b }),
                new ReturnStatement(
                    new InfixOperation(Op.And,
                        Elements.Select(e => Op.Eq.Of(var_a.Get(e), var_b.Get(e))).ToList()
                    )
                )
            );
        }

        public FunctionDefinition GetCompareGenerator() {
            Variable var_leq = new("leq", BitType.Instance);
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, CompareId, new[] { var_a, var_b }),
                new VariableDeclaration(var_leq, Lit0),
                new RepeatStatement(new Hole(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), DisjunctId.Call(var_a, var_b)))
                ),
                new ReturnStatement(var_leq.Ref())
            );
        }

        public FunctionDefinition GetCompareRefinementGenerator(Identifier prevId, Variable budget) {
            Variable var_leq = new("leq", BitType.Instance);
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, CompareId, new[] { var_a, var_b }),
                var_leq.Declare(prevId.Call(var_a, var_b)),
                new RepeatStatement(budget.Ref(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), DisjunctId.Call(var_a, var_b)))
                ),
                Return(var_leq.Ref())
            );
        }

        public FunctionDefinition GetCompareReductionGenerator(Variable budget) {
            Variable var_leq = new("leq", BitType.Instance);
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(BitType.Id, CompareId, new[] { var_a, var_b }),
                var_leq.Declare(Lit0),
                new RepeatStatement(budget.Ref(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), DisjunctId.Call(var_a.Ref(), var_b.Ref())))
                ),
                Return(var_leq.Ref())
            );
        }


        public FunctionDefinition GetDisjunctGenerator() {
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Generator, BitType.Id, DisjunctId, new[] { var_a, var_b }),
                Return(
                    Op.And.Of(Elements.Select(e =>
                        GetAtomFunctionId(e.Type).Call(var_a.Get(e), var_b.Get(e))
                    ).ToList())
                )
            );
        }

        private static Identifier GetAtomFunctionId(IType type) => type switch {
            BitType => BitType.AtomId,
            IntType => IntType.AtomId,
            _ => throw new NotSupportedException(),
        };

        public FunctionDefinition GetNonEqualityHarness() {
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, NonEqId, Array.Empty<Variable>()),
                var_a.Declare(this.NewFromHoles()),
                var_a.Declare(this.NewFromHoles()),
                Assertion(Not(EqId.Call(var_a, var_b))),
                Assertion(CompareId.Call(var_a, var_b))
            );
        }

        public IEnumerable<IStatement> GetPartialEqAssertions(Variable a, Variable b, Variable c) {
            if (a.Type != this || b.Type != this || c.Type != this) throw new ArgumentException();

            yield return new Annotation($"{a.Type}: reflexivity and antisymmetry", 1);
            yield return new AssertStatement(
                Op.Eq.Of(
                    Op.And.Of(
                        CompareId.Call(a, b),
                        CompareId.Call(b, a)
                    ),
                    this.Equal(a, b)
                )
            );

            yield return new Annotation($"{a.Type}: transitivity");
            yield return new AssertStatement(
                Op.Or.Of(
                    Not(CompareId.Call(a, b)),
                    Not(CompareId.Call(b, c)),
                    CompareId.Call(a, c)
                )
            );
        }
    }
}
