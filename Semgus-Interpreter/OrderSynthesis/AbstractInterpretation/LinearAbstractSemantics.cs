using Semgus.MiniParser;
using Semgus.Model;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.Subproblems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    //internal interface ISemanticLibrary {
    //    IReadOnlyList<ISemanticsOfProduction> Productions { get; }
    //}
    //internal interface ISemanticsOfProduction {
    //    int prod_seq { get; }
    //    bool is_deterministic { get; } // true iff at most one of this production's CHCs can hold at a time



    //}

    //internal interface ISemantics {
    //    IReadOnlyList<IOperationalStep> Steps { get; }
    //}

    //internal class GenericInterpreter {
    //    class StackFrame {
    //        private IDSLSyntaxNode node;
    //        private Dictionary<string, dynamic> variables;



    //        public StackFrame(IDSLSyntaxNode node, Dictionary<string, dynamic> top_level_variables) {
    //            this.node = node;
    //            this.variables = top_level_variables;
    //        }



    //    }
    //    private ISemanticLibrary sem;

    //    public GenericInterpreter(ISemanticLibrary the_semantics) {

    //    }

    //    public bool Check(IDSLSyntaxNode node, Dictionary<string,dynamic> top_level_variables, int max_depth) {
    //        var stack = new Stack<StackFrame>();

    //        stack.Push(new(node, top_level_variables));

    //        while(stack.TryPeek(out var frame)) {

    //        }

    //    }
    //}

    // Abstract semantics for a formulaic term
    internal class LinearAbstractSemantics {
        private readonly LinearTermSubtreeAbstraction subtrees;
        private readonly ConcreteTransformer concreteTransformer;
        private readonly IReadOnlyList<Monotonicity> monotonicities;
        private readonly bool anyNonMono;
        private readonly bool[] argNonMono;
        private readonly int _n;

        public LinearAbstractSemantics(LinearTermSubtreeAbstraction subtrees, ConcreteTransformer concreteTransformer, IReadOnlyList<Monotonicity> monotonicities) {
            this.subtrees = subtrees;
            this.concreteTransformer = concreteTransformer;
            this.monotonicities = monotonicities;

            _n = monotonicities.Count;

            anyNonMono = false;
            argNonMono = new bool[_n];
            for (int i = 0; i < _n; i++) {
                anyNonMono |= (argNonMono[i] = monotonicities[i] == Monotonicity.None);
            }
        }

        public bool Interpret(AbstractInterpretationLibrary context, IDSLSyntaxNode node, MuxInterval main_input, out MuxInterval main_output) {

            var ok = subtrees.EvaluateSubtrees(context, node, main_input, out var subtree_output_intervals);

            if (!ok) {
                main_output = default;
                return false;
            }

            long multiplicity = 1;
            bool all_single = true;

            if (anyNonMono) {
                for (int i = 0; i < _n; i++) {
                    if (argNonMono[i]) {
                        if (!subtree_output_intervals[i].IsSingle) {
                            multiplicity *= subtree_output_intervals[i].DiscreteValueCount();
                            all_single = false;
                        } else {
                            all_single &= subtree_output_intervals[i].IsSingle;
                        }
                    }
                    all_single &= (subtree_output_intervals[i].IsSingle);
                }
            } else {
                for (int i = 0; i < _n; i++) {
                    if (!subtree_output_intervals[i].IsSingle) {
                        all_single = false;
                        break;
                    }
                }
            }

            if (all_single) {
                // Evaluate over a single tuple array
                main_output = EvaluateKnownSingle(subtree_output_intervals);
                return true;
            }

            if (multiplicity == 1) {
                // Evaluate over a single interval array
                main_output = EvalOne(subtree_output_intervals);
                return true;
            }

            const long MAX_MULTIPLICITY = 10;

            if (multiplicity > MAX_MULTIPLICITY) {
                // give up and return interval-top
                main_output = MuxInterval.Widest(concreteTransformer.out_tuple_type);
                return true;
            }

            // unify
            main_output = EvalJoinMany(subtree_output_intervals);
            return true;
        }

        private MuxInterval EvaluateKnownSingle(MuxInterval[] subtree_output_intervals) {
            var single_input = new MuxTuple[_n];
            for (int i = 0; i < _n; i++) single_input[i] = subtree_output_intervals[i].Left;
            return new(concreteTransformer.Evaluate(single_input));
        }

        private MuxInterval EvalJoinMany(MuxInterval[] subtree_output_intervals) {
            MuxInterval[][] constituent_options = new MuxInterval[subtree_output_intervals.Length][];
            bool each_all_single = true;

            int k = 0;
            for (int i = 0; i < _n; i++) {
                if (argNonMono[i]) {
                    constituent_options[i] = subtree_output_intervals[i].Split();
                } else {
                    var mu = subtree_output_intervals[i];
                    constituent_options[i] = new[] { mu };
                    each_all_single &= mu.IsSingle;
                }
            }

            bool once = false;
            MuxInterval? joined = null;

            if (each_all_single) {
                foreach (var hot_array in Util.IterationUtil.CartesianProduct(constituent_options)) {
                    var this_result = EvaluateKnownSingle(hot_array);
                    if (once) {
                        joined = MuxInterval.IntervalJoin(joined!, this_result);
                    } else {
                        joined = this_result;
                        once = true;
                    }
                }

            } else {
                foreach (var hot_array in Util.IterationUtil.CartesianProduct(constituent_options)) {
                    var this_result = EvalOne(hot_array);
                    if (once) {
                        joined = MuxInterval.IntervalJoin(joined!, this_result);
                    } else {
                        joined = this_result;
                        once = true;
                    }
                }
            }

            Debug.Assert(once);

            return joined!;
        }

        private MuxInterval EvalOne(MuxInterval[] subtree_output_tuples) {
            var left = new MuxTuple[_n];
            var right = new MuxTuple[_n];

            for (int i = 0; i < monotonicities.Count; i++) {
                var itv = subtree_output_tuples[i];
                switch (monotonicities[i]) {
                    case Monotonicity.Increasing:
                        left[i] = itv.Left;
                        right[i] = itv.Right;
                        break;
                    case Monotonicity.Decreasing:
                        left[i] = itv.Right;
                        right[i] = itv.Left;
                        break;
                    case Monotonicity.None:
                        Debug.Assert(itv.IsSingle); // Conditions other than this should be handled upstream
                        left[i] = itv.Left;
                        right[i] = itv.Left;
                        break;
                }
            }

            var leftResult = concreteTransformer.Evaluate(left);
            var rightResult = concreteTransformer.Evaluate(right);
            return new MuxInterval(leftResult, rightResult);
        }
    }
}
