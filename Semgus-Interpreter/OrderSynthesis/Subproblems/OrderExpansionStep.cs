using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class OrderExpansionStep {
        public int Iter { get; }
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<MonotoneLabeling> MonotoneFunctions { get; }
        public IReadOnlyList<FunctionDefinition> PrevComparisons { get; }
        public IReadOnlyList<Variable> Budgets { get; private set; }

        public OrderExpansionStep(int iter, IReadOnlyList<StructType> structs, IReadOnlyList<MonotoneLabeling> monotoneFunctions, IReadOnlyList<FunctionDefinition> prevComparisons) {
            Iter = iter;
            Structs = structs;
            MonotoneFunctions = monotoneFunctions;
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
                yield return st.GetEqualityFunction();
                yield return PrevComparisons[i];
                yield return st.GetCompareRefinementGenerator(PrevComparisons[i].Id, Budgets[i]);
                yield return st.GetDisjunctGenerator();
            }

            yield return BitType.GetAtom();
            yield return IntType.GetAtom();

            foreach (var fn in MonotoneFunctions) {
                yield return fn.Function;
            }

            yield return GetMain();
        }

        public FunctionDefinition GetMain() {
            var clasps = Clasp.GetAll(MonotoneFunctions.Select(f => f.Function.Signature).Cast<FunctionSignature>());

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = MonotonicityStep.GetMainInitContent(clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList());

            body.AddRange(input_assembly_statements);

            body.Add(new Annotation("Check partial equality properties", 2));
            foreach (var c in clasps) {
                body.AddRange(c.Type.GetPartialEqAssertions(c.Indexed[0], c.Indexed[1], c.Indexed[2]));
            }

            body.Add(new Annotation("Monotonicity", 2));

            var claspMap = clasps.ToDictionary(v => v.Type.Id);
            foreach (var fn in MonotoneFunctions) {
                body.AddRange(GetMonoAssertions(claspMap, fn));
            }

            List<IExpression> expFlagRefs = new();

            for (int i = 0; i < Structs.Count; i++) {
                var type = Structs[i];
                Variable flag = new("exp_" + type.Name, BitType.Instance);
                body.AddRange(GetExpansionAssertions(type, Budgets[i], PrevComparisons[i].Id, flag));
                expFlagRefs.Add(flag.Ref());
            }

            body.Add(new AssertStatement(Op.Or.Of(expFlagRefs)));

            body.Add(new MinimizeStatement(Op.Plus.Of(Budgets.Select(b => b.Ref()).ToList())));

            return new FunctionDefinition(new FunctionSignature(new("main"), FunctionModifier.Harness, VoidType.Instance, input_args), body);
        }

        private static IEnumerable<IStatement> GetExpansionAssertions(StructType type, Variable budget, Identifier prev, Variable expFlag) {
            Variable a = new(type.Name + "_new0", type);
            Variable b = new(type.Name + "_new1", type);

            yield return new AssertStatement(Op.Geq.Of(budget.Ref(), X.L0));

            yield return a.Declare(new StructNew(type.Id, type.Elements.Select(e => e.Assign(new Hole())).ToList()));
            yield return b.Declare(new StructNew(type.Id, type.Elements.Select(e => e.Assign(new Hole())).ToList()));

            yield return expFlag.Declare(Op.And.Of(
                type.CompareId.Call(a.Ref(), b.Ref()),
                UnaryOp.Not.Of(prev.Call(a.Ref(), b.Ref()))
            ));
        }

        private static IEnumerable<IStatement> GetMonoAssertions(IReadOnlyDictionary<Identifier, Clasp> clasps, MonotoneLabeling labeled) {
            var fn = labeled.Function;
            var labels = labeled.ArgMonotonicities;
            if (fn.Signature is not FunctionSignature sig || sig.ReturnType is not StructType type_out) throw new NotSupportedException();

            List<VariableRef> fixed_args = new();

            {
                Counter<Identifier> vcount = new();
                foreach (var v in sig.Args) {
                    var key = v.Type.Id;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
                    vcount.Increment(key);
                }
            }

            yield return new Annotation($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < sig.Args.Count; i++) {
                if (sig.Args[i].Type is not StructType type_i) throw new NotSupportedException();


                if (labels[i] == Monotonicity.None) {
                    yield return new Annotation($"Argument {i}: no monotonicity");
                    continue;
                }
                yield return new Annotation($"Argument {i}: {labels[i]}");

                var alt_i = clasps[type_i.Id].Alternate;

                List<VariableRef> alt_args = new(fixed_args);
                alt_args[i] = alt_i.Ref();

                switch (labels[i]) {
                    case Monotonicity.Increasing:
                        yield return new AssertStatement(
                            type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(fixed_args), fn.Call(alt_args)))
                        );
                        break;
                    case Monotonicity.Decreasing:
                        yield return new AssertStatement(
                            type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(alt_args), fn.Call(fixed_args)))
                        );
                        break;
                }
            }
        }
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);
        public static async Task<Output> ExecuteLoop(FlexPath dir, PipelineState state) {
            var comparisons = state.Comparisons ?? throw new NullReferenceException();
            IReadOnlyList<FunctionDefinition> prev_compare = Array.Empty<FunctionDefinition>();

            const int MAX_REFINEMENT_STEPS = 100;

            int i = 0;
            for (; i < MAX_REFINEMENT_STEPS; i++) {
                prev_compare = comparisons
                    .Select(c => c with { Signature = c.Signature.AsRichSignature(state.TypeMap, new("prev_" + c.Id.Name)) })
                    .ToList();

                var refinement_step = new OrderExpansionStep(i, state.Structs, state.MonotoneFunctions, prev_compare);

                var dir_refinement = dir.Append($"iter_{i}/");

                (var result, var stopFlag) = await refinement_step.Execute(dir_refinement); // May throw
                if (stopFlag) break;
                comparisons = result.Comparisons;
            }

            return new(prev_compare);
        }
        public async Task<(Output output, bool stopFlag)> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");

            System.Console.WriteLine($"--- [Refinement {Iter}] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Refinement {Iter}] Invoking Sketch on {file_in} ---");

            var step2_sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (step2_sketch_result) {
                Console.WriteLine($"--- [Refinement {Iter}] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Refinement {Iter}] Sketch rejected; done with refinement ---");
                return (new(Array.Empty<FunctionDefinition>()), true);
            }

            Console.WriteLine($"--- [Refinement {Iter}] Reading compare functions ---");

            var compare_ids = this.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisons.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), extraction_targets);
            } catch (Exception) {
                Console.WriteLine($"--- [Refinement {Iter}] Failed to extract all comparison functions; halting ---");
                throw;
            }

            Console.WriteLine($"--- [Refinement {Iter}] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = PipelineUtil.Compactify(extracted_functions.Where(f => compare_ids.Contains(f.Id)).ToList(), this.PrevComparisons);
            } catch (Exception) {
                Console.WriteLine($"--- Failed to transform all comparison functions; halting ---");
                throw;
            }

            return (new(compacted), false);
        }
    }
}
