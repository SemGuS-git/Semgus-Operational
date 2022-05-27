using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;
    internal class OrderExpansionStep {
        public record Config(
            IReadOnlyDictionary<Identifier, StructType> StructTypeMap,
            IReadOnlyList<StructType> StructTypeList,
            IReadOnlyList<MonotoneLabeling> MonotoneFunctions,
            IReadOnlyList<FunctionDefinition> PrevComparisons
        );

        public int Iter { get; }
        public IReadOnlyDictionary<Identifier, StructType> StructTypeMap { get; }
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<MonotoneLabeling> MonotoneFunctions { get; }
        public IReadOnlyList<FunctionDefinition> PrevComparisons { get; }
        public IReadOnlyList<Variable> Budgets { get; private set; }

        public OrderExpansionStep(int iter, Config config) {
            Iter = iter;
            (StructTypeMap, Structs, MonotoneFunctions, PrevComparisons) = config;

            Budgets = Structs.Select(s => new Variable($"budget_{s.Id}", IntType.Id)).ToList();

            Debug.Assert(PrevComparisons.All(f => StructTypeMap.TryGetValue(f.Signature.Args[0].TypeId, out var st) && f.Signature.Id != st.CompareId));
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

            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            foreach (var fn in MonotoneFunctions) {
                yield return fn.Function;
            }

            yield return GetMain();
        }

        public FunctionDefinition GetMain() {
            var clasps = Clasp.GetAll(StructTypeMap, MonotoneFunctions.Select(f => f.Function.Signature));

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = MonotonicityStep.GetMainInitContent(clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList());

            body.AddRange(input_assembly_statements);

            body.Add(new Annotation("Check partial equality properties", 2));
            foreach (var c in clasps) {
                body.AddRange(c.Type.GetPartialEqAssertions(c.Indexed[0].Sig(), c.Indexed[1].Sig(), c.Indexed[2].Sig()));
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

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("main"), input_args), body);
        }

        private static IEnumerable<IStatement> GetExpansionAssertions(StructType type, Variable budget, Identifier prev, Variable expFlag) {
            var a = Varn($"{type.Id}_new0", type.Id);
            var b = Varn($"{type.Id}_new1", type.Id);

            yield return new AssertStatement(Op.Geq.Of(budget.Ref(), Lit0));

            yield return a.Declare(new StructNew(type.Id, type.Elements.Select(e => e.Assign(new Hole())).ToList()));
            yield return b.Declare(new StructNew(type.Id, type.Elements.Select(e => e.Assign(new Hole())).ToList()));

            yield return expFlag.Declare(Op.And.Of(
                type.CompareId.Call(a.Ref(), b.Ref()),
                UnaryOp.Not.Of(prev.Call(a.Ref(), b.Ref()))
            ));
        }

        private IEnumerable<IStatement> GetMonoAssertions(IReadOnlyDictionary<Identifier, Clasp> clasps, MonotoneLabeling labeled) {
            var fn = labeled.Function;
            var sig = fn.Signature;
            var labels = labeled.ArgMonotonicities;
            if (!StructTypeMap.TryGetValue(sig.ReturnTypeId, out var type_out)) throw new NotSupportedException();

            List<VariableRef> fixed_args = new();

            {
                Counter<Identifier> vcount = new();
                foreach (var v in sig.Args) {
                    var key = v.TypeId;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
                    vcount.Increment(key);
                }
            }

            yield return new Annotation($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < sig.Args.Count; i++) {
                if (!StructTypeMap.TryGetValue(sig.Args[i].TypeId, out var type_i)) throw new NotSupportedException();

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
                            type_i.CompareId.Call(fixed_args[i], alt_i.Ref()).Implies(type_out.CompareId.Call(fn.Call(fixed_args), fn.Call(alt_args)))
                        );
                        break;
                    case Monotonicity.Decreasing:
                        yield return new AssertStatement(
                            type_i.CompareId.Call(fixed_args[i], alt_i.Ref()).Implies(type_out.CompareId.Call(fn.Call(alt_args), fn.Call(fixed_args)))
                        );
                        break;
                }
            }
        }
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);
        public static async Task<Output> ExecuteLoop(FlexPath dir, PipelineState state, bool reuse_prev = false) {
            var comparisons = state.Comparisons ?? throw new NullReferenceException();
            IReadOnlyList<FunctionDefinition> prev_compare = Array.Empty<FunctionDefinition>();

            const int MAX_REFINEMENT_STEPS = 100;

            //public FunctionSignature AsRichSignature(IReadOnlyDictionary<Identifier, IType> typeDict, Identifier? replacement_id = null)
            //    => new(
            //        Flag,
            //        typeDict[ReturnTypeId],
            //        replacement_id ?? Id,
            //        Args.Select(
            //            a => a is RefVariableDeclaration ?
            //            throw new InvalidOperationException() :
            //            new Variable(a.Id, typeDict[a.TypeId])
            //        ).ToList()
            //    ) { ImplementsId = this.ImplementsId };


            int i = 0;
            for (; i < MAX_REFINEMENT_STEPS; i++) {
                prev_compare = comparisons
                    .Select(c => c with { Signature = c.Signature with { Id = new("prev_" + c.Id.Name) } })
                    .ToList();

                var refinement_step = new OrderExpansionStep(i, new(state.StructTypeMap, state.StructTypeList, state.LabeledTransformers!.Where(tf=>tf.Any).ToList(), prev_compare));

                var dir_refinement = dir.Append($"iter_{i}/");

                (var result, var stopFlag) = await refinement_step.Execute(dir_refinement, reuse_prev); // May throw
                if (stopFlag) break;
                comparisons = result.Comparisons;
            }

            return new(prev_compare);
        }
        public async Task<(Output output, bool stopFlag)> Execute(FlexPath dir, bool reuse_prev = false) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");

            if (reuse_prev) {
                System.Console.WriteLine($"--- [Refinement {Iter}] Reusing previous result ---");
                if(!File.Exists(file_out.PathWin)) {
                    Console.WriteLine($"--- [Refinement {Iter}] No result.sk; done with refinement ---");
                    return (new(Array.Empty<FunctionDefinition>()), true);
                }

            } else { 
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
            }
            Console.WriteLine($"--- [Refinement {Iter}] Reading compare functions ---");

            var compare_ids = this.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisons.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), extraction_targets);
            } catch (Exception) {
                if (reuse_prev) {
                    Console.WriteLine($"--- [Refinement {Iter}] Failed to extract all comparison functions; done with refinement ---");
                    return (new(Array.Empty<FunctionDefinition>()), true);

                } else {
                    Console.WriteLine($"--- [Refinement {Iter}] Failed to extract all comparison functions; halting ---");
                    throw;
                }
            }

            Console.WriteLine($"--- [Refinement {Iter}] Transforming compare functions ---");

            var (to_compact, to_reference) = extracted_functions.GetEnumerator().Partition(f => compare_ids.Contains(f.Id)).ReadToLists();

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = PipelineUtil.ReduceEachToSingleExpression(to_compact, to_reference);
            } catch (Exception) {
                Console.WriteLine($"--- Failed to transform all comparison functions; halting ---");
                throw;
            }

            return (new(compacted), false);
        }
    }
}
