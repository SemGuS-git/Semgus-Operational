using Semgus.Model;
using Semgus.Model.Smt.Terms;
using Semgus.Operational;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Constraints {
    public class InductiveConstraintConverter {
        // TODO: this function should be obviated by fixes to the parser
        private static NtSymbol InferStartSymbol(SemgusTermType termType, SemgusGrammar grammar) {
            var key = termType.StringName();
            var matches = grammar.NonTerminals.Where(nt => nt.Sort.StringName() == key).ToList();

            switch(matches.Count) {
                case 0: throw new KeyNotFoundException("Unable to infer nonterminal matching synth-fun term type");
                case 1: return matches[0].Convert();
                default: throw new InvalidDataException("Ambiguous nonterminal for synth-fun");
            }
        }

        public static InductiveConstraint ProcessConstraints(SemgusSynthFun sf, IEnumerable<SmtTerm> constraintTerms, RelationTracker relations) {
            if (sf.Rank.ReturnSort is not SemgusTermType termType) throw new InvalidDataException("Invalid synth-fun");

            var startSymbol = InferStartSymbol(termType, sf.Grammar);

            List<BehaviorExample> examples = new();

            foreach(var constraint in constraintTerms) {
                if (TryProcessBehaviorExample(sf, constraint, relations, out var example)) {
                    examples.Add(example);
                } else {
                    throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [literal...])");
                }
            }

            return new(startSymbol, termType, examples);
        }

        public static bool IsShapedLikeBehaviorExampleFor(SemgusSynthFun sf, SmtTerm constraintTerm) =>
            constraintTerm is SmtFunctionApplication constraintAppl &&
            constraintAppl.Arguments.Count >= 1 &&
            constraintAppl.Arguments[0] is SmtFunctionApplication sfAppl &&
            sfAppl.Definition.StringName() == sf.Relation.StringName();
        
        static bool TryProcessBehaviorExample(SemgusSynthFun sf, SmtTerm constraintTerm, RelationTracker relations, [NotNullWhen(true)] out BehaviorExample? example) {
            var sfName = sf.Relation.StringName();

            if(
                constraintTerm is not SmtFunctionApplication constraintAppl ||
                constraintAppl.Arguments.Count < 1 ||
                constraintAppl.Arguments[0] is not SmtFunctionApplication sfAppl ||
                sfAppl.Definition.StringName() != sfName ||
                !relations.TryMatchName(constraintAppl.Definition.StringName(), out var rel)
            ) {
                example = null;
                return false;
                throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [literal...])");
            }

            List<object> values = new();
            List<RelationSlotInfo> varInfo = new();

            for(int i = 0; i < rel.Slots.Count; i++) {
                var slot = rel.Slots[i];

                if (slot.Label == RelationSlotLabel.Term) continue;

                if (constraintAppl.Arguments[i] is not SmtLiteral literal) {
                    example = null;
                    return false;
                    throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [literal...])");
                }

                varInfo.Add(slot);
                values.Add(literal.BoxedValue);
            }

            var block = values.ToArray();

            if(block.Length != rel.Slots.Count-1) {
                example = null;
                return false;
            }

            example = new BehaviorExample(varInfo, block);
            return true;
        }
    }
}
