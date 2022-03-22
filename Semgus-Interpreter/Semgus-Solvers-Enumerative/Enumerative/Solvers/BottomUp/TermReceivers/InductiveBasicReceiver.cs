using Semgus.Constraints;
using Semgus.Operational;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semgus.Solvers.Enumerative {
    public class InductiveBasicReceiver : ITermReceiver {
        private readonly InterpreterHost _interpreter;
        private readonly InductiveConstraint _checker;
        private readonly IReadOnlyList<IReduction> _reductions;

        public InductiveBasicReceiver(InterpreterHost interpreter, InductiveConstraint checker, IEnumerable<IReduction> reductions = null) {
            _interpreter = interpreter;
            _checker = checker;
            _reductions = reductions?.ToList() ?? new();
        }

        public TermReceiverCode Receive(IDSLSyntaxNode node) {
            foreach (var reduction in _reductions) {
                if (reduction.CanPrune(node)) return TermReceiverCode.Prune;
            }

            return IsSolution(node) ? TermReceiverCode.ReturnSolution : TermReceiverCode.Retain;
        }

        public bool IsSolution(IDSLSyntaxNode node) {
            // If this node is not a production of the root NT, it cannot satisfy the constraint
            if (!_checker.MatchesNT(node)) return false;

            // If the node is a partial program, it cannot satisfy the constraint
            if (!node.CanEvaluate) return false;

            for (int i = 0; i < _checker.ExampleCount; i++) {
                var result_i = _interpreter.RunProgram(node, _checker.Examples[i].Values);

                if (result_i.HasError) return false;

                // We are safe to check the output values directly from the array, since the nonterminals match
                if (!_checker.TestMatchRaw(result_i.Values, i)) return false;
            }

            return true;
        }

        //public bool IsSolutionDebug(IDSLSyntaxNode node,out string report) {

        //    // If this node is not a production of the root NT, it cannot satisfy the constraint
        //    if (!_checker.MatchesNT(node)) {
        //        report = "Wrong nonterminal";
        //        return false;
        //    }

        //    // If the node is a partial program, it cannot satisfy the constraint
        //    if (!node.CanEvaluate) {
        //        report = "Partial program";
        //        return false;
        //    }

        //    var ok = true;
        //    var sb = new StringBuilder();

        //    for (int i = 0; i < _checker.ExampleCount; i++) {
        //        var input = _checker.Examples[i].Values;
        //        sb.Append($"[{i}] {StringifyDict(input)} -> ");

        //        var result_i = _interpreter.RunProgram(node, input);


        //        if (result_i.HasError) {
        //            ok = false;
        //            sb.AppendLine("!! ERROR");
        //        } else {
        //            var output = _checker.Examples[i].Values;
        //            sb.Append(StringifyArray(result_i.Values.Take(output.Length)));
        //            // We are safe to check the output values directly from the array, since the nonterminals match
        //            if (!_checker.TestMatchRaw(result_i.Values, i)) {
        //                ok = false;
        //                sb.Append(" !! should be ");
        //                sb.AppendLine(StringifyArray(output));
        //            } else {
        //                sb.AppendLine(" ok");
        //            }
        //        }
        //    }
        //    report = sb.ToString();
        //    return ok;
        //}

        public void Dispose() { }

        // lazy - this should be moved to util
        private static string StringifyDict(IReadOnlyDictionary<string, object> input) {
            var sb = new StringBuilder();
            sb.Append('{');
            bool space = false;
            foreach(var kvp in input) {
                if (space) sb.Append(',');
                sb.Append(kvp.Key);
                sb.Append('=');
                sb.Append(kvp.Value.ToString());
                space = true;
            }
            sb.Append('}');
            return sb.ToString();
        }
        // lazy - this should be moved to util
        private static string StringifyArray(IEnumerable<object> input) {
            var sb = new StringBuilder();
            sb.Append('[');
            bool space = false;
            foreach (var value in input) {
                if (space) sb.Append(',');
                sb.Append(value.ToString());
                space = true;
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
