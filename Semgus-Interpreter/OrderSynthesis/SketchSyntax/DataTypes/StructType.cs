using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class StructType : IType {
        public Identifier Id { get; }
        public string Name => Id.Name;

        public string? Comment { get; set; }

        public IReadOnlyList<Variable> Elements { get; }

        public Identifier CompareId { get; }
        public Identifier DisjunctId { get; }
        public Identifier EqId { get; }
        public Identifier NonEqId { get; }

        public StructType(Identifier id, IReadOnlyList<Variable> elements) {
            this.Id = id;
            this.Elements = elements;

            CompareId = new("compare_" + id.Name);
            EqId = new("eq_" + id.Name);
            DisjunctId = new("disjunct_" + id.Name);
            NonEqId = new("non_eq_" + id.Name);
        }

        public override string ToString() => Name;

        public StructDefinition GetStructDef() => new StructDefinition(Id, Elements) { Comment = Comment };

        public FunctionDefinition GetEqualityFunction() {
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(EqId, FunctionModifier.None, BitType.Instance, new[] { var_a, var_b }),
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

            return new FunctionDefinition(new FunctionSignature(CompareId, FunctionModifier.None, BitType.Instance, new[] { var_a, var_b }),
                new VariableDeclaration(var_leq, X.L0),
                new RepeatStatement(new Hole(),
                    var_leq.Assign(Op.Or.Of(var_leq.Ref(), DisjunctId.Call(var_a.Ref(), var_b.Ref())))
                ),
                new ReturnStatement(var_leq.Ref())
            );
        }

        public FunctionDefinition GetDisjunctGenerator() {
            Variable var_a = new("a", this);
            Variable var_b = new("b", this);

            return new FunctionDefinition(new FunctionSignature(DisjunctId, FunctionModifier.Generator, BitType.Instance, new[] { var_a, var_b }),
                new ReturnStatement(
                    new InfixOperation(Op.And, Elements.Select(e =>
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

            return new FunctionDefinition(new FunctionSignature(NonEqId, FunctionModifier.Harness, VoidType.Instance, Array.Empty<Variable>()),
                new VariableDeclaration(var_a, this.New(Elements.Select(e => e.Assign(new Hole())))),
                new VariableDeclaration(var_b, this.New(Elements.Select(e => e.Assign(new Hole())))),
                new AssertStatement(this.NotEqual(var_a, var_b)),
                new AssertStatement(this.Compare(var_a, var_b))
            );
        }

        public IEnumerable<IStatement> GetPartialEqAssertions(Variable a, Variable b, Variable c) {
            if (a.Type != this || b.Type != this || c.Type != this) throw new ArgumentException();

            yield return new LineComment($"{a.Type}: reflexivity and antisymmetry", 1);
            yield return new AssertStatement(
                Op.Eq.Of(
                    Op.And.Of(
                        this.Compare(a, b),
                        this.Compare(b, a)
                    ),
                    this.Equal(a, b)
                )
            );

            yield return new LineComment($"{a.Type}: transitivity");
            yield return new AssertStatement(
                Op.Or.Of(this.NotCompare(a, b), this.NotCompare(b, c), this.Compare(a, c))
            );
        }
    }
}
