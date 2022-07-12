using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;
    internal class OrderExpansionStep {
        //public record Config(
        //    IReadOnlyDictionary<Identifier, StructType> StructTypeMap,
        //    IReadOnlyList<StructType> StructTypeList,
        //    IReadOnlyList<MonotoneLabeling> MonotoneFunctions,
        //    IReadOnlyList<FunctionDefinition> PrevComparisons
        //);



        public int Iter { get; }
        //public IReadOnlyDictionary<Identifier, StructType> StructTypeMap { get; }
        public IReadOnlyList<StructType> Structs { get; }
        //public IReadOnlyList<MonotoneLabeling> MonotoneFunctions { get; }
        public IReadOnlyDictionary<Identifier, FunctionDefinition> PrevComparisonsByStId { get; }

        public IReadOnlyList<AnnotatedQueryFunction> QueryFunctions { get; }
        //public IReadOnlyList<Variable> Budgets { get; private set; }

        public OrderExpansionStep(int iter, IReadOnlyList<StructType> struct_types, IReadOnlyList<FunctionDefinition> comparisons,IReadOnlyList<AnnotatedQueryFunction> queryFunctions) {
            var compare_to_st_map = struct_types.ToDictionary(st => st.CompareId, st => st.Id);

            Iter = iter;
            Structs = struct_types;

            PrevComparisonsByStId = comparisons
                .Select(c => (compare_to_st_map[c.Id], c with { Signature = c.Signature with { Id = new($"prev_{c.Id}") } }))
                .ToDictionary(t=>t.Item1, t=> t.Item2);


            QueryFunctions = queryFunctions;
        }

        

        private record Bundle(StructType st, FunctionDefinition prev_cmp, Variable budget);

        public IEnumerable<IStatement> GetFile() {
            var bundles = Structs.Select(s => new Bundle(s, PrevComparisonsByStId[s.Id], new Variable($"budget_{s.Id}", IntType.Id))).ToList();

            foreach (var b in bundles) {
                var st = b.st;
                yield return b.budget.Declare(new Hole());
                yield return st.GetStructDef();
                yield return st.GetEqualityFunction();
                yield return b.prev_cmp;
                yield return st.GetCompareReductionGenerator(b.budget);
                yield return st.GetDisjunctGenerator();
                yield return st.GetPartialOrderHarness();
                // don't need the non-equivalence harness: this is established by the superset harness + prev step
                yield return st.GetSupersetHarness(b.prev_cmp.Id);
            }


            yield return CompareAtomGenerators.GetBitAtom();
            yield return CompareAtomGenerators.GetIntAtom();

            var sd_dict = Structs.ToDictionary(s => s.Id);
            foreach (var q in QueryFunctions) {
                yield return q.fdef;

                foreach (var harness in GetMonotonicityHarnesses(q.fdef, q.relevant_block_ids, q.mono, sd_dict)) {
                    yield return harness;
                }
            }

            yield return GetExpansionHarness(bundles);
        }


        static IEnumerable<FunctionDefinition> GetMonotonicityHarnesses(FunctionDefinition f, IReadOnlyList<int> relevant_block_ids, IReadOnlyList<Monotonicity> all_monotonicities, IReadOnlyDictionary<Identifier, StructType> struct_types) {
            Debug.Assert(f.Signature.ReturnTypeId == BitType.Id);
            Debug.Assert(f.Signature.Args.Count > 1);
            Debug.Assert(f.Signature.Args.Select((a, i) => (a, i)).All(t => t.a.IsRef == (t.i == 1)));

            foreach (var i in relevant_block_ids) {
                if (i == 1) throw new ArgumentException();
                if (all_monotonicities[i] == Monotonicity.None) continue;
                yield return GetMonotonicityHarness(f, i, struct_types, all_monotonicities[i]);
            }
        }

        static FunctionDefinition GetMonotonicityHarness(FunctionDefinition f, int target_idx, IReadOnlyDictionary<Identifier, StructType> struct_types, Monotonicity mono) {


            var n = f.Signature.Args.Count;

            var outer_args = new List<FunctionArg>();
            var steps = new List<IStatement>();

            var output_st = struct_types[f.Signature.Args[1].TypeId];

            for (int i = 0; i < n; i++) {
                if (i == 1) continue;
                var arg = f.Signature.Args[i];
                struct_types[arg.TypeId].PutConstructionForInputBlock(outer_args, steps, arg.Variable);
            }
            var target_st = struct_types[f.Signature.Args[target_idx].TypeId];
            var alt = new Variable("alt", target_st.Id);
            struct_types[f.Signature.Args[target_idx].TypeId].PutConstructionForInputBlock(outer_args, steps, alt);

            // If !cmp(alt, arg_i) return early
            steps.Add(target_st.CompareId.Call(alt, outer_args[target_idx + 1].Variable).Assume());

            var a_out = new Variable("a_out", output_st.Id);
            var b_out = new Variable("b_out", output_st.Id);

            steps.Add(a_out.Declare());
            steps.Add(b_out.Declare());

            var arg_list_a = new IExpression[n];
            var arg_list_b = new IExpression[n];

            for (int i = 0; i < n; i++) {
                if (i == 1) {
                    arg_list_a[i] = a_out.Ref();
                    arg_list_b[i] = b_out.Ref();
                } else {
                    arg_list_a[i] = outer_args[i].Ref();
                    if (i == target_idx) {
                        arg_list_b[i] = alt.Ref();
                    } else {
                        arg_list_b[i] = outer_args[i].Ref();

                    }
                }
            }

            // make sure the semantics hold
            steps.Add(f.Call(arg_list_a).Assume());
            steps.Add(f.Call(arg_list_b).Assume());

            var test = mono switch {
                Monotonicity.None => throw new ArgumentException(),
                Monotonicity.Increasing => output_st.CompareId.Call(a_out, b_out),
                Monotonicity.Decreasing => output_st.CompareId.Call(b_out, a_out),
                Monotonicity.Constant => output_st.EqId.Call(a_out, b_out),
                _ => throw new ArgumentOutOfRangeException(),
            };

            // assert that the already-known monotonicity property still holds
            steps.Add(test.Assert());

            return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"mono_{f.Id}_v{target_idx}"), outer_args), steps);
        }


        private static FunctionDefinition GetExpansionHarness(IReadOnlyList<Bundle> bundles) {

            List<IStatement> steps = new();

            List<IExpression> expFlagRefs = new();
            foreach(var b in bundles) {
                Variable flag = new($"expand_ord_{b.st.Id}", BitType.Instance);
                steps.AddRange(GetExpansionAssertions(b,flag));
                expFlagRefs.Add(flag.Ref());
            }

            steps.Add(Op.Or.Of(expFlagRefs).Assert());

            // Minimize total budget
            steps.Add(new MinimizeStatement(Op.Plus.Of(bundles.Select(b => b.budget.Ref()).ToList())));

            return new FunctionDefinition(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("require_expansion")), steps);
        }

        private static IEnumerable<IStatement> GetExpansionAssertions(Bundle bundle, Variable expFlag) {
            var (type, prev, budget) = bundle;
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

        //private IEnumerable<IStatement> GetMonoAssertions(IReadOnlyDictionary<Identifier, Clasp> clasps, MonotoneLabeling labeled) {
        //    var fn = labeled.Function;
        //    var sig = fn.Signature;
        //    var labels = labeled.ArgMonotonicities;
        //    if (!StructTypeMap.TryGetValue(sig.ReturnTypeId, out var type_out)) throw new NotSupportedException();

        //    List<VariableRef> fixed_args = new();

        //    {
        //        Counter<Identifier> vcount = new();
        //        foreach (var v in sig.Args) {
        //            var key = v.TypeId;
        //            fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
        //            vcount.Increment(key);
        //        }
        //    }

        //    yield return new Annotation($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

        //    for (int i = 0; i < sig.Args.Count; i++) {
        //        if (!StructTypeMap.TryGetValue(sig.Args[i].TypeId, out var type_i)) throw new NotSupportedException();

        //        if (labels[i] == Monotonicity.None) {
        //            yield return new Annotation($"Argument {i}: no monotonicity");
        //            continue;
        //        }
        //        yield return new Annotation($"Argument {i}: {labels[i]}");

        //        var alt_i = clasps[type_i.Id].Alternate;

        //        List<VariableRef> alt_args = new(fixed_args);
        //        alt_args[i] = alt_i.Ref();

        //        switch (labels[i]) {
        //            case Monotonicity.Increasing:
        //                yield return new AssertStatement(
        //                    type_i.CompareId.Call(fixed_args[i], alt_i.Ref()).Implies(type_out.CompareId.Call(fn.Call(fixed_args), fn.Call(alt_args)))
        //                );
        //                break;
        //            case Monotonicity.Decreasing:
        //                yield return new AssertStatement(
        //                    type_i.CompareId.Call(fixed_args[i], alt_i.Ref()).Implies(type_out.CompareId.Call(fn.Call(alt_args), fn.Call(fixed_args)))
        //                );
        //                break;
        //        }
        //    }
        //}
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);
        public static async Task<Output> ExecuteLoop(FlexPath dir, MonotonicityStep.Output prior, bool reuse_prev = false) {

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
            var comparisons = prior.Comparisons;
            for (; i < MAX_REFINEMENT_STEPS; i++) {
                var refinement_step = new OrderExpansionStep(i, prior.StructDefs, comparisons, prior.QueryFunctions);

                var dir_refinement = dir.Append($"iter_{i}/");

                (var result, var stopFlag) = await refinement_step.Execute(dir_refinement, reuse_prev); // May throw
                if (stopFlag) break;
                comparisons = result.Comparisons;
            }

            return new(comparisons);
        }

        public async Task<(Output output, bool stopFlag)> Execute(FlexPath dir, bool reuse_prev = false) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");

            if (reuse_prev) {
                System.Console.WriteLine($"--- [Refinement {Iter}] Reusing previous result ---");
                if (!File.Exists(file_out.PathWin)) {
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

            var extraction_targets = compare_ids.Concat(this.PrevComparisonsByStId.Values.Select(p => p.Id));
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
