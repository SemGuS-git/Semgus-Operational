using Semgus.Operational;
using Semgus.Model;
using Semgus.Model.Smt.Terms;
using static Semgus.Model.SemgusChc;
using System.Diagnostics.CodeAnalysis;

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

                var name = bind.StringName();
                if (info.Name == Subject.Name) {
                    throw new InvalidDataException($"Attempting to use \"{info.Name}\" as child term of \"{Subject.Name}\"; this is not permitted (in {chc.Head})");
                }

                d.Add(info.Name, info);
            }

            nameMap = d;
        }

        public bool TryMatch(SmtVariable v, [NotNullWhen(true)] out TermVariableInfo? info) => nameMap.TryGetValue(v.StringName(), out info);

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
