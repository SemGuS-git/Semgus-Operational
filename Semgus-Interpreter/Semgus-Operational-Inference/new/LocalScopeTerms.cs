using Semgus.Interpretation;
using Semgus.Model;
using Semgus.Model.Smt.Terms;
using static Semgus.Model.SemgusChc;

namespace Semgus {
    public class LocalScopeTerms {
        public TermVariableInfo Subject { get; }
        private readonly IReadOnlyDictionary<string, TermVariableInfo> nameMap;

        public LocalScopeTerms(SemgusChc chc) {
            Dictionary<string, TermVariableInfo> d = new();
            Subject = GetSubject(chc.Head);
            d.Add(Subject.Name, Subject);

            int i = 1;
            foreach (var match_bind in chc.Binder.Bindings) {
                var bind = match_bind.Binding;
                if (bind.Sort is not SemgusTermType stt) throw new NotSupportedException();
                var info = new TermVariableInfo(bind.StringName(), i++, stt.StringName());
                d.Add(info.Name, info);
            }

            nameMap = d;
        }

        public bool TryMatch(SmtVariable v, out TermVariableInfo info) => nameMap.TryGetValue(v.StringName(), out info);

        private static TermVariableInfo GetSubject(SemanticRelation rel) {
            TermVariableInfo? subject = null;
            foreach (var arg in rel.Arguments) {
                if (arg.Sort is not SemgusTermType stt) continue;
                if (subject is not null) throw new NotSupportedException("Semantic relation of more than one term");
                subject = new TermVariableInfo(arg.StringName(), 0, stt.StringName());
            }
            return subject ?? throw new NotSupportedException("Semantic relation with no term");
        }
    }
}
