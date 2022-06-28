using Semgus.Model;
using Semgus.Operational;
using System.Collections.Generic;

namespace Semgus.Constraints {
    public class InductiveConstraint {
        public NtSymbol StartSymbol { get; }
        public SemgusTermType TermType { get; }
        public IReadOnlyList<BehaviorExample> Examples { get; }
        public int ExampleCount { get; }

        public InductiveConstraint(NtSymbol startSymbol, SemgusTermType termType, IReadOnlyList<BehaviorExample> examples) {
            StartSymbol = startSymbol;
            TermType = termType;
            Examples = examples;
            ExampleCount = examples.Count;
        }

        public bool TestMatchRaw(object[] rawValues, int exampleIdx) {
            var expected = Examples[exampleIdx].Values;
            
            for(int i = 0; i < expected.Length; i++) {
                if (!expected[i].Equals(rawValues[i])) return false;
            }

            return true;
        }

        public bool MatchesNT(IDSLSyntaxNode node) => node.Nonterminal == StartSymbol;

        //public string PrintResultSummary(InterpreterHost interpreter, IDSLSyntaxNode node) {
        //    var sb = new StringBuilder();

        //    sb.AppendLine($"Testing {node}");

        //    var matchesNT = MatchesNT(node);
        //    var all = matchesNT;

        //    for (int i = 0; i < ExampleCount; i++) {
        //        sb.Append("  ");
        //        var example = Examples[i];
        //        var result = interpreter.RunProgram(node, example.LegacyInput);
        //        if (result.HasError) {
        //            all = false;
        //            WriteError(sb, example, result.Error);
        //            sb.AppendLine(")");
        //            continue;
        //        }

        //        var output = result.Output;
                
        //        if (matchesNT) {
        //            var ok = MatchesOutput(output, i);
        //            sb.Append(ok ? "[X] " : "[ ] ");
        //            all &= ok;
        //        } else {
        //            sb.Append("[~] ");
        //        }
                
        //        sb.Append(example.ToString());
        //        sb.Append(" | (");

        //        if (matchesNT) {
        //            for (int j = 0; j < _outputCount; j++) {
        //                var key = _argVariables.OutputVariables[j].Name;
        //                sb.Append(key);
        //                sb.Append(": ");
        //                sb.Append(output[j]);
        //                sb.Append(", ");
        //            }
        //        } else {
        //            int k = -1;
        //            for (int j = 0; j < output.Count; j++) {
        //                do {
        //                    k++;
        //                } while (node.ArgumentSlots[k].IsInput);
        //                sb.Append(node.ArgumentSlots[k].Name);
        //                sb.Append(": ");
        //                sb.Append(output[j]);
        //                sb.Append(", ");
        //            }
        //        }


        //        sb.AppendLine(")");
        //    }
        //    sb.AppendLine(matchesNT ? (all ? "PASS" : "FAIL") : "N/A");
        //    return sb.ToString();
        //}

        //private static void WriteError(StringBuilder sb, BehaviorExample example, InterpreterErrorInfo error) {
        //    sb.Append("[!] ");
        //    sb.Append(example.ToString());
        //    sb.Append(" | ERROR: ");
        //    sb.Append(error.ToString());
        //}
    }
}
