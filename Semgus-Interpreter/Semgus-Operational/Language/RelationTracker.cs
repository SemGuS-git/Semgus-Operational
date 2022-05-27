using Semgus.Model;
using Semgus.Model.Smt.Terms;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Semgus.Model.SemgusChc;

namespace Semgus {
    public class RelationTracker {
        private readonly IReadOnlyDictionary<string, RelationInfo> _relationMap;
        private readonly IReadOnlyDictionary<string, string> _termToRelationKeyMap;

        public RelationTracker(IEnumerable<SemgusChc> chcs) {
            var main = new Dictionary<string, RelationInfo>();
            var keyMap = new Dictionary<string, string>();

            RelationSlotLabel infer_label(SemgusChc chc, SmtVariable argVar) {
                if (argVar.Sort is SemgusTermType) return RelationSlotLabel.Term;
                if (chc.IsInputVar(argVar)) return RelationSlotLabel.Input;
                if (chc.IsOutputVar(argVar)) return RelationSlotLabel.Output;
                throw new InvalidDataException("Only terms, input vars, and output vars are permitted in head relation");
            }

            foreach (var chc in chcs) {
                var head = chc.Head;
                var relKey = head.Relation.Name.AsString();

                int n = head.Arguments.Count;
                if (head.Rank.Arity != n) throw new InvalidDataException();


                var slots = new RelationSlotInfo[n];

                for (int i = 0; i < n; i++) {
                    var arg = head.Arguments[i];
                    if (!ReferenceEquals(arg.Sort, head.Rank.ArgumentSorts[i])) throw new InvalidDataException("Variable in semantic relation has incorrect sort");
                    slots[i] = new RelationSlotInfo(Sort: arg.Sort, TopLevelVarName:arg.Name.AsString(), Label: infer_label(chc, arg));
                }

                var relInfo = new RelationInfo(relKey, slots);

                {
                    if (main.TryGetValue(relKey, out var preexisting)) {
                        if (!preexisting.Equals(relInfo)) throw new InvalidDataException($"Multiple conflicting definitions for semantic relation {relKey}");
                    } else {
                        main.Add(relKey, relInfo);
                    }
                }
                {
                    var ttkey = chc.Binder.ParentType.Name.AsString();
                    if (!keyMap.TryAdd(ttkey,relKey)) {
                        Debug.Assert(keyMap[ttkey] == relKey);
                    }
                }
            }

            _relationMap = main;
            _termToRelationKeyMap = keyMap;
        }

        public RelationInfo GetRelation(SemgusTermType termType) => _relationMap[_termToRelationKeyMap[termType.Name.AsString()]];
        public RelationInfo GetRelation(string termTypeKey) => _relationMap[_termToRelationKeyMap[termTypeKey]];
        

        public bool TryMatch(SemanticRelation rel, [NotNullWhen(true)]out RelationInfo? info) {
            return _relationMap.TryGetValue(rel.Relation.Name.AsString(), out info);
        }
        public bool TryMatchName(string name, [NotNullWhen(true)]out RelationInfo? info) {
            return _relationMap.TryGetValue(name, out info);
        }
    }
}
