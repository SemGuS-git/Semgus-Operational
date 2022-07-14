using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semgus.Model;
using Semgus.Operational;
using Semgus.OrderSynthesis.Subproblems;
using System.Diagnostics;
using Semgus.Model.Smt;
using Semgus.OrderSynthesis.AbstractInterpretation;

namespace Semgus.OrderSynthesis.IntervalSemantics {

    public record BlockItemRef(int block_id, int slot);

    public static class Extensions {
        public static IEnumerable<T> SkipAt<T>(this IEnumerable<T> e, int idx) {
            var rator = e.GetEnumerator();

            for(int i = 0; i < idx; i++) {
                if (!rator.MoveNext()) yield break;
                yield return rator.Current;
            }
            if (!rator.MoveNext()) yield break;
            while(rator.MoveNext()) {
                yield return rator.Current;
            }
        }

        public static bool AllElementsEqual<T>(this IEnumerable<T> e) where T : IEquatable<T> {
            var enumerator = e.GetEnumerator();

            if (!enumerator.MoveNext()) return true; // empty
            var basis = enumerator.Current;

            while (enumerator.MoveNext()) {
                if (!enumerator.Current.Equals(basis)) return false;
            }
            return true;
        }
        public static bool IsSequentialFromZero(this IEnumerable<int> e) {
            int i = 0;
            foreach (var j in e) {
                if (i != j) return false;
                i++;
            }
            return true;
        }

        public static string GetKey(this SemgusTermType stt) => stt.Name.Name.Symbol;

        public static string ToStringAsTuple<T>(this IEnumerable<T> list) {
            var sb = new StringBuilder();
            return '('+ string.Join(", ", list)+')';
        }
    }

    public interface IBlockStep { }
    public record BlockAssert(IReadOnlyList<int> RequiredBlockId, IReadOnlyList<Monotonicity> MonoLabels, IBlockExpression Expression) : IBlockStep;

    public record BlockEval(int TermIndex, int InputBlockId, int OutputBlockId) : IBlockStep;

    public enum TypeLabel {
        Any,
        Int,
        Bool,
    }

    public record BlockAssign(int TargetBlockId, IReadOnlyList<int> RequiredBlocks, IReadOnlyList<Monotonicity> Monotonicities, IReadOnlyList<IBlockExpression> Exprs) : IBlockStep;

    public interface IBlockExpression { }

    public record BlockExprRead(BlockItemRef Address) : IBlockExpression;
    public record BlockExprCall(string FunctionName, List<IBlockExpression> Args) : IBlockExpression;
    public record BlockExprLiteral(TypeLabel TypeLabel, object Value) : IBlockExpression;


    public record BlockDef(int Id, IReadOnlyList<TypeLabel> Members) {
        public int Size => Members.Count;
    }

    public record BlockProduction(int TermTypeId, string Name, List<BlockSemantics> Semantics);
    public record BlockSemantics(List<int> BlockTypes, List<IBlockStep> Steps) {
        internal BlockSemantics WithTfMonotonicities(IReadOnlyList<Monotonicity> mono) {
            List<IBlockStep> new_steps = new();
            foreach(var step in Steps) {
                new_steps.Add(step switch {
                    BlockAssign assign =>  assign with { Monotonicities = assign.RequiredBlocks.Select(a=>mono[a]).ToList() },
                    _=>step,
                });
            }
            return this with{ Steps = new_steps};
        }
    }

    public class TypeHelper {
        public IReadOnlyDictionary<string, int> TermTypeIds { get; }
        public IReadOnlyList<BlockDef> BlockTypes { get; }
        public IReadOnlyList<(int type_id_in, int type_id_out)> TermTypeInputOutput { get; }

        public TypeHelper(IReadOnlyDictionary<string, int> termTypeIds, IReadOnlyList<BlockDef> blockTypes, IReadOnlyList<(int type_id_in, int type_id_out)> termTypeInputOutput) {
            TermTypeIds = termTypeIds;
            BlockTypes = blockTypes;
            TermTypeInputOutput = termTypeInputOutput;
        }

        public BlockDef GetInputBlockType(SemgusTermType term_type) => GetInputBlockType(TermTypeIds[term_type.Name.Name.Symbol]);
        public BlockDef GetOutputBlockType(SemgusTermType term_type) => GetOutputBlockType(TermTypeIds[term_type.Name.Name.Symbol]);
        public BlockDef GetInputBlockType(int term_type_id) => BlockTypes[TermTypeInputOutput[term_type_id].type_id_in];
        public BlockDef GetOutputBlockType(int term_type_id) => BlockTypes[TermTypeInputOutput[term_type_id].type_id_out];

        public int GetTermTypeId(SemgusTermType term_type) {
            return TermTypeIds[term_type.Name.Name.Symbol];
        }

        class BlockShape : IEquatable<BlockShape?> {
            public IReadOnlyList<TypeLabel> Types { get; }

            public BlockShape(IReadOnlyList<TypeLabel> types) {
                Types = types;
            }

            public static BlockShape From(IReadOnlyList<VariableInfo> variables) {
                return new(variables.Select(v => ConvertSort(v.Sort)).ToList());
            }


            public override int GetHashCode() {
                var h = new HashCode();
                foreach (var a in Types) h.Add(a);
                return h.ToHashCode();
            }

            public override string? ToString() {
                return this.Types.ToStringAsTuple();
            }

            public override bool Equals(object? obj) {
                return Equals(obj as BlockShape);
            }

            public bool Equals(BlockShape? other) {
                return other != null && Types.SequenceEqual(other.Types);
            }

            public static bool operator ==(BlockShape? left, BlockShape? right) {
                return EqualityComparer<BlockShape>.Default.Equals(left, right);
            }

            public static bool operator !=(BlockShape? left, BlockShape? right) {
                return !(left == right);
            }
        }


        public static TypeHelper From(IReadOnlyDictionary<string, int> term_type_ids, IReadOnlyList<ProductionRuleInterpreter> relevant_productions) {
            // Merge all tuple types with the same shape
            Dictionary<(int tt_id, bool is_out), int> tt_io_block_ids = new();

            Dictionary<BlockShape, int> block_ids = new();

            void insert_shape_or_assert_eq(int tt_id, bool is_out, IReadOnlyList<VariableInfo> variables) {
                var shape = BlockShape.From(variables);
                if (block_ids.TryGetValue(shape, out var block_id)) {
                    if (tt_io_block_ids.TryGetValue((tt_id, is_out), out var prior_block_id)) {
                        if (block_id != prior_block_id) throw new InvalidDataException();
                    } else {
                        tt_io_block_ids.Add((tt_id, is_out), block_id);
                    }
                } else {
                    block_id = block_ids.Count;
                    block_ids.Add(shape, block_id);
                    tt_io_block_ids.TryAdd((tt_id, is_out), block_id);
                }
            }

            // Scan shapes
            foreach (var prod in relevant_productions) {
                var ttk = prod.TermType.GetKey();
                int tt_id = term_type_ids[ttk];
    
                insert_shape_or_assert_eq(tt_id, false, prod.InputVariables);
                insert_shape_or_assert_eq(tt_id, true, prod.OutputVariables);
            }

            List<(int type_id_in, int type_id_out)> io_list = new();

            for (int i = 0; i < term_type_ids.Count; i++) {
                io_list.Add((tt_io_block_ids[(i, false)], tt_io_block_ids[(i, true)]));
            }


            var block_types = Sequentialize(block_ids).Select((a, i) => new BlockDef(i, a.Types)).ToList();


            return new(term_type_ids, block_types, io_list);
        }
        public BlockProduction GetBlockAbstraction(ProductionRuleInterpreter prod) {
            var term_type = this.GetTermTypeId(prod.TermType);
            var name = prod.ToString();
            var semantics = prod.Semantics.Select(a => GetBlockAbstraction(a)).ToList();
            return new(term_type, name, semantics);
        }

        public BlockSemantics GetBlockAbstraction(SemanticRuleInterpreter sem) {
            var prod = sem.ProductionRule;
            var term_types = prod.SyntaxConstructor.Children.Cast<SemgusTermType>().Prepend(sem.ProductionRule.TermType)
                .ToList();

            var block_types = new List<BlockDef>() {
                this.GetInputBlockType(term_types[0]),
                this.GetOutputBlockType(term_types[0])
            };

            Debug.Assert(prod.InputVariables.Count == block_types[0].Size);
            Debug.Assert(prod.OutputVariables.Count == block_types[1].Size);

            sem.ProductionRule.SyntaxConstructor.Children.Select(ch => ch.Name);

            List<IBlockStep> abs_steps = new();
            Dictionary<BlockItemRef, (List<int> req, IBlockExpression expr)> individual_assignments = new();


            // this map is populated only as variables are defined
            var variable_id_map = new Dictionary<string, BlockItemRef>();
            var tuple_type_map = new Dictionary<int, int>();

            // input tuple at idx 0
            for (int i = 0; i < prod.InputVariables.Count; i++) {
                var iv = prod.InputVariables[i];
                variable_id_map.Add(iv.Name, new(0, i));
            }
            // output tuple at idx 1
            for (int i = 0; i < prod.OutputVariables.Count; i++) {
                var iv = prod.OutputVariables[i];
                variable_id_map.Add(iv.Name, new(1, i));
            }

            bool sem_output_is_passthrough = false;

            foreach (var step in sem.Steps) {
                switch (step) {
                    case TermEvaluation eval:
                        var input_id = GetTupleIdx(eval.InputVariables, variable_id_map);

                        // Forbid using output block as an input to child eval 
                        // (might not be necessary)
                        Debug.Assert(input_id != 1);

                        int output_id;

                        // Check whether this eval's output is same as our sem's output
                        if (variable_id_map.TryGetValue(eval.OutputVariables[0].Name, out var first)) {
                            Debug.Assert(first.block_id == 1); // if the output block is known, we require it to be the sem output
                            Debug.Assert(first.slot == 0);

                            var output_block_type = this.GetOutputBlockType(term_types[eval.Term.Index]);
                            Debug.Assert(block_types[1] == output_block_type);

                            Debug.Assert(!sem_output_is_passthrough); // throw if we're already assigning output from another term eval
                            Debug.Assert(individual_assignments.Count == 0); // throw if we're already assigning output from a formula
                            sem_output_is_passthrough = true;
                            output_id = 1;

                            for (int i = 0; i < eval.OutputVariables.Count; i++) {
                                var ov = eval.OutputVariables[i];
                                var uv = variable_id_map[ov.Name];
                                Debug.Assert(uv.block_id == 1);
                                Debug.Assert(uv.slot == i);
                            }
                        } else {
                            // Declare new output block
                            output_id = block_types.Count;
                            var output_block_type = this.GetOutputBlockType(term_types[eval.Term.Index]);
                            block_types.Add(output_block_type);

                            for (int i = 0; i < eval.OutputVariables.Count; i++) {
                                var ov = eval.OutputVariables[i];
                                Debug.Assert(!variable_id_map.ContainsKey(ov.Name));
                                variable_id_map.Add(ov.Name, new(output_id, i));
                            }
                        }

                        abs_steps.Add(new BlockEval(eval.Term.Index, input_id, output_id));
                        break;
                    case ConditionalAssertion assert:
                        HashSet<int> req_set = new();
                        var e = ScanExpression(assert.Expression, variable_id_map, req_set);
                        Debug.Assert(!req_set.Contains(1)); // forbid reference to sem's output variables

                        var req = req_set.ToList();
                        req.Sort();

                        var mono = Enumerable.Repeat(Monotonicity.None, req.Count).ToList();

                        abs_steps.Add(new BlockAssert(req, mono, e));
                        break;
                    case AssignmentFromLocalFormula assign:
                        var target = variable_id_map[assign.ResultVar.Name];

                        Debug.Assert(target.block_id == 1); // only permit assignment to output tuple
                        Debug.Assert(!sem_output_is_passthrough); // throw if we're already assigning output from a term eval

                        HashSet<int> req_set1 = new();
                        var e1 = ScanExpression(assign.Expression, variable_id_map, req_set1);
                        Debug.Assert(!req_set1.Contains(1)); // forbid reference to other output variables
                        var req1 = req_set1.ToList();

                        individual_assignments.Add(target, (req1, e1));
                        break;
                }
            }

            if (sem_output_is_passthrough) {
                Debug.Assert(individual_assignments.Count == 0); //redundant
            } else {
                abs_steps.Add(CollateAssignments(1, block_types[1].Size, individual_assignments));
            }

            return new(block_types.Select(a => a.Id).ToList(), abs_steps);

        }
        public static T[] Sequentialize<T>(IReadOnlyDictionary<T, int> dict) {
            var n = dict.Count;
            var array = new T[n];

            foreach (var kvp in dict) {
                if (kvp.Value < 0 || kvp.Value >= n) throw new ArgumentException("Input is not properly indexed");
                array[kvp.Value] = kvp.Key;
            }

            return array;
        }

        internal static BlockAssign CollateAssignments(int block_id, int block_size, IReadOnlyDictionary<BlockItemRef, (List<int>, IBlockExpression)> individual_assignments) {
            var req_set = new HashSet<int>();

            var exprs = new List<IBlockExpression>();

            for (int i = 0; i < block_size; i++) {
                var (i_req, i_expr) = individual_assignments[new(block_id, i)];
                req_set.UnionWith(i_req);
                exprs.Add(i_expr);
            }

            var req = req_set.ToList();
            req.Sort();

            var mono = Enumerable.Repeat(Monotonicity.None, req_set.Count).ToList();

            return new BlockAssign(block_id, req, mono, exprs);

        }

        internal static TypeLabel ConvertSort(Model.Smt.SmtSort sort) {
            if (sort.Name == SmtCommonIdentifiers.BoolSortId) return TypeLabel.Bool;
            if (sort.Name == SmtCommonIdentifiers.IntSortId) return TypeLabel.Int;
            throw new NotSupportedException();
        }

        internal static IBlockExpression ScanExpression(ISmtLibExpression expression, Dictionary<string, BlockItemRef> variable_id_map, HashSet<int> required_tuple_ids) {
            switch (expression) {
                case FunctionCallExpression e:
                    return new BlockExprCall(e.Function.Name, e.Args.Select(a => ScanExpression(a, variable_id_map, required_tuple_ids)).ToList());
                case LiteralExpression lit:
                    return new BlockExprLiteral(ConvertSort(lit.Sort), lit.BoxedValue);
                case VariableEvalExpression var_eval:
                    var v_id = variable_id_map[var_eval.Variable.Name];
                    required_tuple_ids.Add(v_id.block_id);
                    return new BlockExprRead(v_id);
                default:
                    throw new NotSupportedException();
            }
        }

        internal static int GetTupleIdx(IReadOnlyCollection<VariableInfo> constituents, IReadOnlyDictionary<string, BlockItemRef> variable_id_map) {
            var a = constituents.Select(iv => variable_id_map[iv.Name]).ToList();

            Debug.Assert(a.Count > 0);
            Debug.Assert(a.Select(e => e.block_id).AllElementsEqual());
            Debug.Assert(a.Select(e => e.slot).IsSequentialFromZero());

            return a[0].block_id;
        }
    }

    public record FlatNt(string Name, int TermType, List<FlatGrammarProd> Productions);
    public record FlatGrammarProd(int prod_idx, List<int> child_nt_ids);

    public class InitialStuff {
        public InitialStuff(List<FlatNt> flat_nts, List<BlockProduction> block_prod, TypeHelper type_helper, int start_symbol) {
            Nonterminals = flat_nts;
            Productions = block_prod;
            TypeHelper = type_helper;
            StartSymbol = start_symbol;
        }

        public List<FlatNt> Nonterminals { get; }
        public List<BlockProduction> Productions { get; }
        public TypeHelper TypeHelper { get; }
        public int StartSymbol { get; }

        public static InitialStuff From(Operational.InterpretationGrammar grammar, Operational.InterpretationLibrary lib) {
 

            Dictionary<ProductionRuleInterpreter, int> all_prod_dict = new();

            var nt_ids = grammar.Nonterminals.Select((a, i) => (a, i)).ToDictionary(a => a.a, a => a.i);

            List<FlatNt> flat_nts = new();
            Dictionary<string, int> term_type_ids = new();

            // Merge all tuple types with the same shape
            foreach (var kvp in grammar.Productions) {
                List<FlatGrammarProd> fgpl = new();
                int? nt_term_type_id = null;
                foreach (var nt_prod in kvp.Value) {

                    int prod_id;
                    if (!all_prod_dict.TryGetValue(nt_prod.Production, out prod_id)) {
                        prod_id = all_prod_dict.Count;
                        all_prod_dict.Add(nt_prod.Production, prod_id);
                    }
                    fgpl.Add(new(prod_id, nt_prod.ChildNonterminals.Select(a => nt_ids[a]).ToList()));


                    var ttk = nt_prod.Production.TermType.GetKey();

                    if (term_type_ids.TryGetValue(ttk,out var tti)) {
                        Debug.Assert(nt_term_type_id!.Value == tti);
                    } else {
                        tti = term_type_ids.Count;
                        term_type_ids.Add(ttk, tti);
                        nt_term_type_id = tti;
                    }   
                }

                flat_nts.Add(new(kvp.Key.Name, nt_term_type_id!.Value, fgpl));
            }

            var all_prod = TypeHelper.Sequentialize(all_prod_dict);

            var type_helper = TypeHelper.From(term_type_ids, all_prod);

            var block_prod = all_prod.Select(type_helper.GetBlockAbstraction).ToList();

            var start_symbol = 0;

            return new(flat_nts, block_prod, type_helper, start_symbol);
        }

        internal InitialStuff WithMonotonicitiesFrom(IReadOnlyList<AnnotatedQueryFunction> queryFunctions) {
            var ok = this.Productions.ToList();

            foreach(var aqf in queryFunctions) {
                var (prod_idx,sem_idx) = aqf.sem_addr;
                var src_prod = Productions[prod_idx];
                var src_sem = src_prod.Semantics[sem_idx];

                ok[prod_idx].Semantics[sem_idx] = src_sem.WithTfMonotonicities(aqf.mono);
            }

            return new(this.Nonterminals, ok, this.TypeHelper, this.StartSymbol);
        }
    }
}
