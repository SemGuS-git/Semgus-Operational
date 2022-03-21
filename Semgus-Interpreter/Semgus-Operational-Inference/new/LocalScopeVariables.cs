using Semgus.Interpretation;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;

namespace Semgus {
    public class LocalScopeVariables {
        public IReadOnlyList<VariableInfo> Variables { get; }

        private readonly IReadOnlyDictionary<string, VariableInfo> nameMap;

        public bool TryMatch(SmtVariable v, out VariableInfo info) => nameMap.TryGetValue(v.StringName(), out info);

        public LocalScopeVariables(SemgusChc chc) {
            List<VariableInfo> r = new();
            int i = 0;
            foreach (var v in chc.InputVariables) r.Add(new(v.StringName(), i++, v.Sort, VariableUsage.Input));
            foreach (var v in chc.OutputVariables) r.Add(new(v.StringName(), i++, v.Sort, VariableUsage.Output));

            foreach (var vb in chc.VariableBindings) {
                if (vb.BindingType != SmtVariableBindingType.Universal) throw new NotSupportedException();
                if (vb.Sort is SemgusTermType) continue; // Skip reference to self term
                if (chc.IsInputVar(vb) || chc.IsOutputVar(vb)) continue; // Skip refere
                r.Add(new(vb.StringName(), i++, vb.Sort, VariableUsage.Auxiliary));
            }

            Dictionary<string, VariableInfo> m = new();

            foreach (var t in r) {
                m.Add(t.Name, t);
            }

            Variables = r;
            nameMap = m;
        }

        internal IEnumerable<VariableInfo> Inputs => Variables.Where(v => v.Usage == VariableUsage.Input);
        internal IEnumerable<VariableInfo> Outputs => Variables.Where(v => v.Usage == VariableUsage.Output);
    }
}
