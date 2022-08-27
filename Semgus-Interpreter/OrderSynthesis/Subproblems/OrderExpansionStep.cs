using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using Semgus.Util.Misc;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    using static Sugar;
    internal class OrderExpansionStep {
        public int Iter { get; }
        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<StructType> StructsToOrder { get; }
        public IReadOnlyDictionary<Identifier, FunctionDefinition> PrevComparisonsByStId { get; }

        public IReadOnlyList<AnnotatedQueryFunction> QueryFunctions { get; }

        public OrderExpansionStep(int iter, IReadOnlyList<StructType> struct_types, IReadOnlyList<StructType> struct_types_to_order, IReadOnlyList<FunctionDefinition> comparisons,IReadOnlyList<AnnotatedQueryFunction> queryFunctions) {
            var compare_to_st_map = struct_types_to_order.ToDictionary(st => st.CompareId, st => st.Id);

            Iter = iter;
            Structs = struct_types;
            StructsToOrder = struct_types_to_order;

            PrevComparisonsByStId = comparisons
                .Select(c => (compare_to_st_map[c.Id], c with { Signature = c.Signature with { Id = new($"prev_{c.Id}") } }))
                .ToDictionary(t=>t.Item1, t=> t.Item2);


            QueryFunctions = queryFunctions;
        }

        

        private record Bundle(StructType st, FunctionDefinition prev_cmp, Variable budget);

        public IEnumerable<IStatement> GetFile() {
            foreach(var st in Structs) {
                yield return st.GetStructDef();
            }

            var bundles = StructsToOrder.Select(s => new Bundle(s, PrevComparisonsByStId[s.Id], new Variable($"budget_{s.Id}", IntType.Id))).ToList();

            foreach (var b in bundles) {
                var st = b.st;
                yield return b.budget.Declare(new Hole());
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
                foreach(var a in q.query.preconditions.Append(q.query.output_transformer)) {
                    yield return a;
                }

                foreach (var harness in GetMonotonicityHarnesses(q, sd_dict)) {
                    yield return harness;
                }
            }

            yield return GetExpansionHarness(bundles);
        }


        static IEnumerable<FunctionDefinition> GetMonotonicityHarnesses(AnnotatedQueryFunction aq, IReadOnlyDictionary<Identifier, StructType> struct_types) {
            Debug.Assert(!aq.query.relevant_block_ids.Contains(1));

            foreach (var i in aq.query.relevant_block_ids) {
                if (aq.mono[i] == Monotonicity.None) continue;
                yield return GetMonotonicityHarness(aq.query, i, struct_types, aq.mono[i]);
            }
        }

        static FunctionDefinition GetMonotonicityHarness(
            MonoQueryBundle query,
            int target_block_id,
            IReadOnlyDictionary<Identifier, StructType> struct_types, 
            Monotonicity mono
        ) {
            static int convert_index(int i) => i > 1 ? i - 1 : i;
            
            var target_idx = convert_index(target_block_id);

            var f = query.output_transformer;

            var args = f.Signature.Args;
            var n = args.Count;

            var raw_outer_args = new List<FunctionArg>();
            var steps = new List<IStatement>();


            var target_st = struct_types[f.Signature.Args[target_idx].TypeId];
            var output_st = struct_types[f.Signature.ReturnTypeId];

            static Variable fab(Identifier type_id, int i) => new($"{type_id}_{i}", type_id);

            var instances = new DictOfList<Identifier, Variable>();

            // create extra instance for the varying argument
            instances.SafeGetCollection(target_st.Id).Add(fab(target_st.Id, 0));

            // make instances
            for (int i = 0; i < n; i++) {
                var t = f.Signature.Args[i].TypeId;
                var l = instances.SafeGetCollection(t);
                l.Add(fab(t, l.Count));
            }

            foreach(var kvp in instances) {
                foreach(var obj in kvp.Value) {
                    struct_types[kvp.Key].PutConstructionForInputBlock(raw_outer_args, steps, obj);
                }
            }

            var arg_list_a = new IExpression[n];
            var arg_list_b = new IExpression[n];


            var a_in = instances[target_st.Id][0];
            var b_in = instances[target_st.Id][1];

            var ctr = new Counter<Identifier>();
            ctr.Init(target_st.Id, 2);

            for (int i = 0; i < n; i++) {
                if (i == target_idx) {
                    arg_list_a[i] = a_in.Ref();
                    arg_list_b[i] = b_in.Ref();
                } else {
                    var u = args[i].TypeId;
                    arg_list_a[i] = instances[u][ctr.Increment(u) - 1].Ref();
                    arg_list_b[i] = arg_list_a[i];
                }
            }

            IExpression inputs_ord = target_st.CompareId.Call(a_in, b_in);
            var guard = query.preconditions.Count == 0 ? inputs_ord :
                Op.And.Of(query.preconditions.SelectMany(p => new IExpression[] { p.Call(arg_list_a), p.Call(arg_list_b) }).Append(inputs_ord).ToList());

            var a_out = query.output_transformer.Call(arg_list_a);
            var b_out = query.output_transformer.Call(arg_list_b);

            var test = mono switch {
                Monotonicity.Increasing => output_st.CompareId.Call(a_out, b_out),
                Monotonicity.Decreasing => output_st.CompareId.Call(b_out, a_out),
                _ => throw new ArgumentException(),
            };

            steps.Add(Op.Or.Of(UnaryOp.Not.Of(guard), test).Assert());

            return new FunctionDefinition(new(FunctionModifier.Harness, VoidType.Id, new($"mono_{f.Id}_v{target_block_id}"), raw_outer_args), steps);
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
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons);
        public static async Task<Output> ExecuteLoop(FlexPath dir, MonotonicityStep.Output prior) {

            const int MAX_REFINEMENT_STEPS = 100;

            int i = 0;
            var comparisons = prior.Comparisons;
            for (; i < MAX_REFINEMENT_STEPS; i++) {
                var refinement_step = new OrderExpansionStep(i, prior.StructDefs, prior.StructDefsToOrder, comparisons, prior.QueryFunctions);

                var dir_refinement = dir.Append($"iter_{i}/");

                (var result, var stopFlag) = await refinement_step.Execute(dir_refinement); // May throw
                if (stopFlag) break;
                comparisons = result.Comparisons;
            }

            return new(comparisons);
        }

        public async Task<(Output output, bool stopFlag)> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.Value);

            var file_in = dir / "input.sk";
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");

            System.Console.WriteLine($"--- [Refinement {Iter}] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.Value)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Refinement {Iter}] Invoking Sketch on {file_in} ---");


            var (sketch_ok, sketch_out) = await IpcUtil.RunSketch(dir, "input.sk", "result.holes.xml");

            _ = Task.Run(() => File.WriteAllText(file_out.Value, sketch_out));

            if (sketch_ok) {
                Console.WriteLine($"--- [Refinement {Iter}] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Refinement {Iter}] Sketch rejected; done with refinement ---");
                return (new(Array.Empty<FunctionDefinition>()), true);
            }

            Console.WriteLine($"--- [Refinement {Iter}] Reading compare functions ---");

            var compare_ids = this.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(this.PrevComparisonsByStId.Values.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = PipelineUtil.ReadSelectedFunctions(sketch_out, extraction_targets);
            } catch (Exception) {
                Console.WriteLine($"--- [Refinement {Iter}] Failed to extract all comparison functions; halting ---");
                throw;
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
