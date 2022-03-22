using Semgus.Operational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semgus.Constraints {
    /// <summary>
    /// Encodes a single input/output example.
    /// </summary>
    public class BehaviorExample {
        /// <summary>
        /// Information about the variables fixed by this example.
        /// Each item describes the variable for the Value at the same index.
        /// </summary>
        public IReadOnlyList<RelationSlotInfo> VariableSlots { get; }

        /// <summary>
        /// Concrete variable values.
        /// </summary>
        public object[] Values { get; }

        //public IReadOnlyDictionary<string,object> LegacyInput { get; }

        public BehaviorExample(IReadOnlyList<RelationSlotInfo> variableSlots, object[] values) {
            if (variableSlots.Count != values.Length) throw new InvalidDataException();
            VariableSlots = variableSlots;
            Values = values;
            //LegacyInput = InputVariables.ToDictionary(info => info.Name, info => values[info.Index]);
        }

        //public bool IsValid() {
        //    int n_in = InputVariables.Count;
        //    int n_out = OutputVariables.Count;
        //    int n = n_in + n_out;

        //    if (n != Values.Length) return false;
        //    bool[] chk = new bool[n];

        //    foreach(var info in InputVariables) {
        //        if (info.Usage != VariableUsage.Input) return false;
        //        var i = info.Index;
        //        if (i < 0 || i >= n) return false;
        //        if (chk[i]) return false;
        //        chk[i] = true;
        //    }
        //    foreach (var info in OutputVariables) {
        //        if (info.Usage != VariableUsage.Output) return false;
        //        var i = info.Index;
        //        if (i < 0 || i >= n) return false;
        //        if (chk[i]) return false;
        //        chk[i] = true;
        //    }

        //    return true;
        //    // todo type checks
        //}

        public override string ToString() {
            var sb = new StringBuilder();

            static void Append(StringBuilder sb, RelationSlotInfo info, object value) {
                sb.Append(info.TopLevelVarName);
                if (info.Label == RelationSlotLabel.Output) sb.Append(":out");
                sb.Append(" = ");
                sb.Append(value.ToString());
            }

            sb.Append('(');
            if (Values.Length > 0) {
                Append(sb, VariableSlots[0], Values[0]);
                for (int i = 1; i < Values.Length; i++) {
                    sb.Append(", ");
                    Append(sb, VariableSlots[i], Values[i]);
                }
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}