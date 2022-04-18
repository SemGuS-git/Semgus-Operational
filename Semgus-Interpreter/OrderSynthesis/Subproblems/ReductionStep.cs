using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class ReductionStep {
        internal record ReductionClasp(StructType Type, Variable A, Variable B);
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<FunctionDefinition> PrevComparisons { get; }
        public IReadOnlyList<Variable> Budgets { get; private set; }

        public ReductionStep(IReadOnlyList<StructType> structs, IReadOnlyList<FunctionDefinition> prevComparisons) {
            Structs = structs;
            PrevComparisons = prevComparisons;

            Budgets = structs.Select(s => new Variable("budget_" + s.Name, IntType.Instance)).ToList();

            if (prevComparisons.Any(f => f.Signature is not FunctionSignature sig || sig.Args[0].Type is not StructType st || sig.Id == st.CompareId)) {
                throw new ArgumentException();
            }
        }


        public IEnumerable<IStatement> GetFile() {
            foreach (var b in Budgets) {
                yield return b.Declare(new Hole());
            }

            foreach (var st in Structs) {
                yield return st.GetStructDef();
            }
            for (int i = 0; i < Structs.Count; i++) {
                StructType? st = Structs[i];
                yield return PrevComparisons[i];
                yield return st.GetCompareReductionGenerator(Budgets[i]);
                yield return st.GetDisjunctGenerator();
            }

            yield return BitType.GetAtom();
            yield return IntType.GetAtom();

            yield return GetMain();
        }


        public FunctionDefinition GetMain() {
            var clasps = Structs.Select(s => new ReductionClasp(s, new Variable(s.Name + "_s0", s), new Variable(s.Name + "_s1", s))).ToList();

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = FirstStep.GetMainInitContent(clasps.SelectMany(c => new[] { c.A, c.B }).ToList());

            body.AddRange(input_assembly_statements);

            foreach (var (clasp, prev) in clasps.Zip(PrevComparisons)) {
                body.Add(new AssertStatement(
                    Op.Eq.Of(
                        clasp.Type.CompareId.Call(clasp.A.Ref(), clasp.B.Ref()),
                        prev.Call(clasp.A.Ref(), clasp.B.Ref())
                        )
                 ));
            }

            body.Add(new MinimizeStatement(Op.Plus.Of(Budgets.Select(b => b.Ref()).ToList())));

            return new FunctionDefinition(new FunctionSignature(new("main"), FunctionModifier.Harness, VoidType.Instance, input_args), body);
        }
    }
}
