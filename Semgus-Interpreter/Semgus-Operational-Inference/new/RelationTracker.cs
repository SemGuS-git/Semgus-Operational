using Semgus.Model;
using Semgus.Model.Smt.Terms;
using static Semgus.Model.SemgusChc;

namespace Semgus {
    internal class RelationTracker {
        private readonly IReadOnlyDictionary<string, RelationInfo> _relationMap;

        public RelationTracker(IEnumerable<SemgusChc> chcs) {
            var d = new Dictionary<string, RelationInfo>();

            RelationSlotLabel infer_label(SemgusChc chc, SmtVariable argVar) {
                if (argVar.Sort is SemgusTermType) return RelationSlotLabel.Term;
                if (chc.IsInputVar(argVar)) return RelationSlotLabel.Input;
                if (chc.IsOutputVar(argVar)) return RelationSlotLabel.Output;
                throw new InvalidDataException("Only terms, input vars, and output vars are permitted in head relation");
            }

            foreach (var chc in chcs) {
                var head = chc.Head;
                var key = head.Relation.StringName();

                int n = head.Arguments.Count;
                if (head.Rank.Arity != n) throw new InvalidDataException();


                var slots = new RelationSlotInfo[n];

                for (int i = 0; i < n; i++) {
                    var arg = head.Arguments[i];
                    if (!ReferenceEquals(arg.Sort, head.Rank.ArgumentSorts[i])) throw new InvalidDataException("Variable in semantic relation has incorrect sort");
                    slots[i] = new RelationSlotInfo(Sort: arg.Sort, Label: infer_label(chc, arg));
                }

                var relInfo = new RelationInfo(key, slots);

                if(d.TryGetValue(key,out var preexisting)) {
                    if (!preexisting.Equals(relInfo)) throw new InvalidDataException($"Multiple conflicting definitions for semantic relation {key}");
                } else {
                    d.Add(key, relInfo);
                }
            }

            _relationMap = d;
        }

        public bool TryMatch(SemanticRelation rel, out RelationInfo info) {
            return _relationMap.TryGetValue(rel.Relation.StringName(), out info);
        }
    }
}
