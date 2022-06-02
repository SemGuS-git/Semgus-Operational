using Semgus.Operational;
using Semgus.OrderSynthesis.Subproblems;

namespace Semgus.OrderSynthesis.AbstractInterpretation {


    using Id = MiniParser.Identifier;
    using Sort = Model.Smt.SmtSort;
    using Bundle = TermTypeTupleIds;
    using SType = SketchSyntax.StructType;

    internal record TermTypeTupleIds(Id inputTuple, Id outputTuple);


    internal class AbstractPreInit {
        record MiniHelper(string TermTypeKey, bool IsInputElseOutput);

        public IReadOnlyList<(ProductionRuleInterpreter, LinearTermSubtreeAbstraction, ConcreteTransformerCore)> List { get; }

        public IReadOnlyDictionary<Id, SType> UniqueTupleTypes { get; }

        private IReadOnlyDictionary<string, Bundle> HelperDict { get; }

        private IReadOnlyDictionary<NtSymbol, string> NonterminalTermTypes { get; }


        public AbstractPreInit(
            IReadOnlyList<(ProductionRuleInterpreter, LinearTermSubtreeAbstraction, ConcreteTransformerCore)> pre_transformers,
            IReadOnlyDictionary<Id, SType> tuple_types,
            IReadOnlyDictionary<string, Bundle> helperDict,
            IReadOnlyDictionary<NtSymbol, string> nonterminalTermTypes
        ) {
            List = pre_transformers;
            UniqueTupleTypes = tuple_types;
            HelperDict = helperDict;
            NonterminalTermTypes = nonterminalTermTypes;
        }

        internal (SType input, SType output) GetIOStructs(Model.SemgusTermType termType) {
            var (key_in, key_out) = HelperDict[termType.Name.Name.Symbol];
            return (UniqueTupleTypes[key_in], UniqueTupleTypes[key_out]);
        }
        internal (SType input, SType output) GetIOStructs(TermVariableInfo info) {
            var (key_in, key_out) = HelperDict[info.TermTypeKey];
            return (UniqueTupleTypes[key_in], UniqueTupleTypes[key_out]);
        }

        // These functions must be in the same order as in the original List
        public AbstractInterpretationLibrary Hydrate(IReadOnlyList<LatticeDefs> lattice, IReadOnlyList<MonotoneLabeling> labeledFunctions) {

            var latticeDict = lattice.ToDictionary(l => l.type.Id, MuxTupleType.Make);

            Dictionary<string, (MuxTupleType input, MuxTupleType output)> types_by_term_type = new();

            foreach (var (_, ttk) in NonterminalTermTypes) {
                if (types_by_term_type.ContainsKey(ttk)) continue;
                var (id_in, id_out) = HelperDict[ttk];

                var ty_in = latticeDict[id_in];
                var ty_out = latticeDict[id_out];

                types_by_term_type.Add(ttk, (ty_in, ty_out));
            }

            List<LinearAbstractSemantics> sem = new();

            foreach (var tau in List.Zip(labeledFunctions)) {
                var ((prod, ltsa, ctc), mono) = tau;

                var tupletype_out = latticeDict[mono.Function.Signature.ReturnTypeId];

                var ct = ctc.Hydrate(tupletype_out);

                sem.Add(new LinearAbstractSemantics(ltsa, ct, mono.ArgMonotonicities));
            }

            Dictionary<NtSymbol, MuxInterval> hole_values = new();
            foreach (var (nt, ttk) in NonterminalTermTypes) {
                hole_values.Add(nt, MuxInterval.Widest(types_by_term_type[ttk].output));
            }

            return new(sem, types_by_term_type, hole_values);
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

            //// The point of *this* rigamarole is to construct equivalence classes of tuple types based on their usage.
            EquivalenceClasses<MiniHelper, Id> eqc = new();
            HashSet<string> unseen_ttks = new();

            for (int i = 0; i < lib.TermTypes.Count; i++) {
                Model.SemgusTermType termType = lib.TermTypes[i];
                var ttk = termType.Name.Name.Symbol;
                eqc.Add(new(ttk, true), new($"ttype{i}_in"));
                eqc.Add(new(ttk, false), new($"ttype{i}_out"));
                unseen_ttks.Add(ttk);
            }


            var main_list = lib.Productions.Where(all_prod.Remove).Select(prod => {
                if (prod.Semantics.Count > 1) throw new NotSupportedException(); // TODO support multiple semantics via branching 
                var (ltsc, ctc) = ExtractLinearCore(prod.Semantics[0], eqc);

                unseen_ttks.Remove(prod.TermType.Name.Name.Symbol);

                return (prod, ltsc, ctc);
            }).ToList();

            // Remove unused struct ids, just to be safe
            foreach (var ttk in unseen_ttks) {
                eqc.Remove(new(ttk, true));
                eqc.Remove(new(ttk, false));
            }

            Dictionary<Id, SketchSyntax.StructType> distinct_structs = new();

            var shuck = lib.TermTypes.ToDictionary(a => a.Name.Name.Symbol);
            foreach (var (keys, struct_id) in eqc.Enumerate()) {
                var (some_ttk, isInputElseOutput) = keys.First();

                var some_rel = lib.SemanticRelations.GetRelation(shuck[some_ttk]);

                var relevant_slots = some_rel.Slots
                    .Where(
                        isInputElseOutput
                        ? (s => s.Label == RelationSlotLabel.Input)
                        : (s => s.Label == RelationSlotLabel.Output)
                    );

                distinct_structs.Add(struct_id, new(
                    struct_id,
                    relevant_slots.Select(
                        (s, i) => new SketchSyntax.Variable($"v{i}", MapSortToPrimTypeId(s.Sort))
                    ).ToList()
                ));
            }

            Dictionary<string, Bundle> wett = new();

            foreach (var mu in lib.TermTypes) {
                var key = mu.Name.Name.Symbol;
                if (unseen_ttks.Contains(key)) continue;
                wett.Add(key, new(eqc[new(key, true)], eqc[new(key, false)]));
            }

            return new(main_list, distinct_structs, wett, nt_to_ttk);
        }

        static Id MapSortToPrimTypeId(Sort sort) {
            if (sort.Name == Model.Smt.SmtCommonIdentifiers.BoolSortId) return SketchSyntax.BitType.Id;
            if (sort.Name == Model.Smt.SmtCommonIdentifiers.IntSortId) return SketchSyntax.IntType.Id;
            throw new NotSupportedException();
        }

        private static (LinearTermSubtreeAbstraction, ConcreteTransformerCore) ExtractLinearCore(SemanticRuleInterpreter sem, EquivalenceClasses<MiniHelper, Id> eqc) {
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

            List<ISmtLibExpression> assertions = new();

            Dictionary<int, ISmtLibExpression> main_output_expressions = new();
            HashSet<int> tuples_in_concrete_transformer = new();

            List<MiniHelper> subtree_tuple_type_keys = new();

            subtree_tuple_type_keys.Add(new(prod.TermType.Name.Name.Symbol, true)); // add input tuple at 0 index

            foreach (var step in sem.Steps) {
                switch (step) {
                    case ConditionalAssertion condat: 
                        assertions.Add(condat.Expression);
                        break;
                    case TermEvaluation eval: {

                            // Scan input variables
                            var input_tuple_idx = GetIndexOfInputTuple(prod.InputVariables, var_index_to_tuple_indices);

                            // Scan and map output variables
                            var output_variables = eval.OutputVariables;

                            int output_tuple_idx = subtree_tuple_type_keys.Count;

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

                            var tk_src = subtree_tuple_type_keys[input_tuple_idx];
                            MiniHelper tk_in = new(eval.Term.TermTypeKey, true);
                            MiniHelper tk_out = new(eval.Term.TermTypeKey, false);

                            // Merge the expected input struct type with the actual one
                            // We need this to avoid an implicit transformer (via casting) of the eval's input tuple
                            eqc.Merge(tk_src, tk_in);

                            subtree_tuple_type_keys.Add(tk_out);

                            abstract_evals.Add(new(eval, eval.Term.Index, input_tuple_idx, output_tuple_idx));
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
