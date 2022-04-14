using Semgus.OrderSynthesis.SketchSyntax.Sugar;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class StructType : IType {
        public StructId Id { get; }
        public string Name => Id.Name;

        public string? Comment { get; set; }

        public IReadOnlyList<VarId> Elements { get; }

        public FunctionId CompareId { get; }
        public FunctionId DisjunctId { get; }
        public FunctionId EqId { get; }
        public FunctionId NonEqId { get; }

        public StructType(StructId id, IReadOnlyList<VarId> elements) {
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
            VarId var_a = new("a", this);
            VarId var_b = new("b", this);

            return new FunctionDefinition(EqId, FunctionFlag.None, BitType.Instance, new[] { var_a, var_b },
                new ReturnStatement(
                    new InfixOperation(Op.And,
                        Elements.Select(e => Op.Eq.Of(new PropertyAccess(var_a, e), new PropertyAccess(var_b, e))).ToList()
                    )
                )
            );
        }

        public FunctionDefinition GetCompareGenerator() {
            VarId var_leq = new("leq", BitType.Instance);
            VarId var_a = new("a", this);
            VarId var_b = new("b", this);

            return new FunctionDefinition(CompareId, FunctionFlag.None, BitType.Instance, new[] { var_a, var_b },
                new VarDeclare(var_leq, X.L0),
                new RepeatStatement(new Hole(),
                    var_leq.Set(Op.Or.Of(var_leq, DisjunctId.Call(var_a, var_b)))
                ),
                new ReturnStatement(var_leq)
            );
        }

        public FunctionDefinition GetDisjunctGenerator() {
            VarId var_a = new("a", this);
            VarId var_b = new("b", this);

            return new FunctionDefinition(DisjunctId, FunctionFlag.Generator, BitType.Instance, new[] { var_a, var_b },
                new ReturnStatement(
                    new InfixOperation(Op.And, Elements.Select(e =>
                        GetAtomFunctionId(e.Type).Call(var_a.Prop(e), var_b.Prop(e))
                    ).ToList())
                )
            );
        }

        private static FunctionId GetAtomFunctionId(IType type) => type switch {
            BitType => BitType.AtomId,
            IntType => IntType.AtomId,
            _ => throw new NotSupportedException(),
        };

        public FunctionDefinition GetNonEqualityHarness() {
            VarId var_a = new("a", this);
            VarId var_b = new("b", this);

            return new FunctionDefinition(NonEqId, FunctionFlag.Harness, VoidType.Instance, Array.Empty<VarId>(),
                new VarDeclare(var_a, new NewExpression(this, Elements.Select(e => new Assignment(e, new Hole())).ToList())),
                new VarDeclare(var_b, new NewExpression(this, Elements.Select(e => new Assignment(e, new Hole())).ToList())),
                new AssertStatement(this.NotEqual(var_a, var_b)),
                new AssertStatement(this.Compare(var_a, var_b))
            );
        }

        public IEnumerable<IStatement> GetPartialEqAssertions(VarId a, VarId b, VarId c) {
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
