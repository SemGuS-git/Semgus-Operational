using Semgus.Operational;
using Semgus.OrderSynthesis.Subproblems;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class AbstractPreInit {
        public IReadOnlyList<(ProductionRuleInterpreter, LinearTermSubtreeAbstraction, ConcreteTransformerCore)> List { get; }

        public IReadOnlyDictionary<TupleId, IReadOnlyList<Model.Smt.SmtSort>> UniqueTupleTypes { get; }

        public AliasMap<TupleId> Resolver { get; }

        private IReadOnlyDictionary<NtSymbol, string> NonterminalTermTypes { get; }

        public AbstractPreInit(
            IReadOnlyList<(ProductionRuleInterpreter, LinearTermSubtreeAbstraction, ConcreteTransformerCore)> pre_transformers,
            IReadOnlyDictionary<TupleId, IReadOnlyList<Model.Smt.SmtSort>> tuple_types,
            AliasMap<TupleId> resolver,
            IReadOnlyDictionary<NtSymbol, string> nonterminalTermTypes
        ) {
            List = pre_transformers;
            UniqueTupleTypes = tuple_types;
            Resolver = resolver;
            NonterminalTermTypes = nonterminalTermTypes;
        }

        // These functions must be in the same order as in the original List
        public AbstractInterpretationLibrary Hydrate(IReadOnlyList<LatticeDefs> lattice, IReadOnlyList<MonotoneLabeling> labeledFunctions) {

            var latticeDict = lattice.ToDictionary(l => l.type.Id, MuxTupleType.Make);

            Dictionary<string, MuxTupleType> types_by_term_type = new();

            List<LinearAbstractSemantics> sem = new();
            foreach (var tau in List.Zip(labeledFunctions)) {
                var ((prod, ltsa, ctc), mono) = tau;



                var tupletype_out = latticeDict[mono.Function.Signature.ReturnTypeId];

                types_by_term_type.TryAdd(prod.TermType.Name.Name.Symbol, tupletype_out);
                var ct = ctc.Hydrate(tupletype_out);

                sem.Add(new LinearAbstractSemantics(ltsa, ct, mono.ArgMonotonicities));
            }

            Dictionary<NtSymbol, MuxInterval> hole_values = new();
            foreach (var (nt, ttk) in NonterminalTermTypes) {
                hole_values.Add(nt, MuxInterval.Widest(types_by_term_type[ttk]));
            }

            return new(sem, hole_values);
        }


        private static T[] Sequentialize<T>(IReadOnlyDictionary<int, T> dict) {
            var n = dict.Count;
            var array = new T[n];

            foreach (var kvp in dict) {
                if (kvp.Key < 0 || kvp.Key >= n) throw new ArgumentException("Input is not properly indexed");
                array[kvp.Key] = kvp.Value;
            }

            return array;
        }

        public static AbstractPreInit From(Operational.InterpretationGrammar grammar, Operational.InterpretationLibrary lib) {

            var nt_to_ttk = grammar.Productions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0].Production.TermType.Name.Name.Symbol);

            // Include only the productions in this grammar
            HashSet<ProductionRuleInterpreter> all_prod = new();
            foreach (var prod in grammar.Productions.Values.SelectMany(val => val.Select(mu => mu.Production))) {
                all_prod.Add(prod);
            }

            // The point of *this* rigamarole is to construct equivalence classes of tuple types based on their usage.
            var tuple_aliases = new AliasMap<TupleId>();
            Dictionary<string, ProductionRuleInterpreter> representatives = new();


            var main_list = lib.Productions.Where(all_prod.Remove).Select(prod => {
                if (prod.Semantics.Count > 1) throw new NotSupportedException(); // TODO support multiple semantics via branching 
                var (ltsc, ctc) = ExtractLinearCore(prod.Semantics[0], tuple_aliases);
                representatives.TryAdd(prod.TermType.Name.Name.Symbol, prod);

                return (prod, ltsc, ctc);
            }).ToList();

            Dictionary<TupleId, IReadOnlyList<Model.Smt.SmtSort>> pre_tuples = new();

            foreach (var (ttkey, prod) in representatives) {
                var tk_in = new TupleId(ttkey, true);
                if (!tuple_aliases.IsAlias(tk_in)) {
                    pre_tuples.Add(tk_in, prod.InputVariables.Select(iv => iv.Sort).ToList());
                }

                var tk_out = new TupleId(ttkey, false);
                if (!tuple_aliases.IsAlias(tk_out)) {
                    pre_tuples.Add(tk_out, prod.OutputVariables.Select(iv => iv.Sort).ToList());
                }
            }

            return new(main_list, pre_tuples, tuple_aliases, nt_to_ttk);
        }


        public static (LinearTermSubtreeAbstraction, ConcreteTransformerCore) ExtractLinearCore(SemanticRuleInterpreter sem, AliasMap<TupleId> aliases) {
            var prod = sem.ProductionRule;

            // Maps the overall indices of variables to the subtree-tuple location that sources them.
            Dictionary<int, (int tupleIdx, int propIdx)> var_index_to_tuple_indices = new();

            // Register the input variables as a tuple at tuple index 0.
            for (int field_idx = 0; field_idx < prod.InputVariables.Count; field_idx++) {
                var_index_to_tuple_indices.Add(prod.InputVariables[field_idx].Index, (0, field_idx));
            }

            // Maps the overall indices of main-output variables to their location in the main-output tuple.
            Dictionary<int, int> var_index_to_output_field_index = new();
            for (int field_idx = 0; field_idx < prod.OutputVariables.Count; field_idx++) {
                var j = prod.OutputVariables[field_idx].Index;
                var_index_to_output_field_index.Add(j, field_idx);
                var_index_to_tuple_indices.Add(j, (-1, j));
            }

            List<AbstractTermEvalStep> abstract_evals = new();

            Dictionary<int, ISmtLibExpression> main_output_expressions = new();
            HashSet<int> tuples_in_concrete_transformer = new();

            List<TupleId> subtree_tuples = new();

            subtree_tuples.Add(aliases.Resolve(new(prod.TermType.Name.Name.Symbol, true)));

            foreach (var step in sem.Steps) {
                switch (step) {
                    case ConditionalAssertion: throw new NotImplementedException();
                    case TermEvaluation eval: {

                            // Scan input variables
                            var input_tuple_idx = GetIndexOfInputTuple(prod.InputVariables, var_index_to_tuple_indices);

                            // Scan and map output variables
                            var output_variables = eval.OutputVariables;

                            int output_tuple_idx = subtree_tuples.Count;

                            for (int field_idx = 0; field_idx < output_variables.Count; field_idx++) {
                                var v = output_variables[field_idx];

                                if (var_index_to_output_field_index.TryGetValue(v.Index, out var main_out_field_idx)) {

                                    // We will be setting main_output[main_out_field_idx] = sub_output[out_tuple_idx][field_idx]
                                    // Implicitly use an extra variable for this

                                    var expr = new VariableEvalExpression(v);
                                    main_output_expressions.Add(main_out_field_idx, expr);

                                    tuples_in_concrete_transformer.Add(output_tuple_idx); // register that this tuple is involved in the main-output transformer
                                }

                                var_index_to_tuple_indices.Add(v.Index, (output_tuple_idx, field_idx));
                            }

                            var tk_src = subtree_tuples[input_tuple_idx];
                            var tk_in = aliases.Resolve(new TupleId(eval.Term.TermTypeKey, true));
                            var tk_out = aliases.Resolve(new TupleId(eval.Term.TermTypeKey, false));

                            // Add alias - we need this to avoid an implicit transformer (via casting) of the eval's input tuple
                            aliases.Register(tk_in, tk_src);

                            subtree_tuples.Add(tk_out);

                            abstract_evals.Add(new(eval, eval.Term.Index, input_tuple_idx, output_tuple_idx, tk_src, tk_out));
                        }
                        break;
                    case AssignmentFromLocalFormula assign: {
                            var target_idx = assign.ResultVar.Index;
                            if (!var_index_to_output_field_index.TryGetValue(target_idx, out var main_out_field_idx))
                                throw new NotSupportedException("Nonlinear semantics: a non-output variable is assigned");

                            main_output_expressions.Add(main_out_field_idx, assign.Expression);

                            foreach (var v in assign.DependencyVariables) {
                                var (tuple_idx, _) = var_index_to_tuple_indices[v.Index]; // throws if this variable doesn't come from some tuple
                                tuples_in_concrete_transformer.Add(tuple_idx); // register that this tuple is involved in the main-output transformer
                            }
                        }
                        break;
                }
            }


            return (
                new(abstract_evals),
                new(
                    Sequentialize(main_output_expressions),
                    Sequentialize(var_index_to_tuple_indices),
                    tuples_in_concrete_transformer
                )
            );
        }

        // Check that all of an eval's input variables are the properties of a single tuple (in the same order), and return that tuple's index
        private static int GetIndexOfInputTuple(IReadOnlyList<VariableInfo> input_variables, Dictionary<int, (int tupleIdx, int propIdx)> var_index_to_tuple_indices) {
            if (input_variables.Count == 0) throw new NotSupportedException("Term type has no input variables; at least one is required here");

            var (tuple_idx, k0) = var_index_to_tuple_indices[input_variables[0].Index]; // throws if this variable doesn't come from some tuple

            if (k0 != 0) throw new NotSupportedException("Nonlinear semantics");

            for (int i = 1; i < input_variables.Count; i++) {
                var (tuple_idx_j, k) = var_index_to_tuple_indices[input_variables[i].Index]; // throws if this variable doesn't come from some tuple
                if (tuple_idx_j != tuple_idx || k != i) throw new NotSupportedException("Nonlinear semantics");
            }

            return tuple_idx;
        }
    }
}
