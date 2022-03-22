using Semgus.Model;
using Semgus.Model.Smt.Terms;
using Semgus.Operational;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Constraints {
    public class InductiveConstraintConverter {
        private readonly ITheoryImplementation _theory;
        private readonly SemgusSynthFun _synthFun;
        private readonly RelationTracker _relations;

        public InductiveConstraintConverter(ITheoryImplementation theory, SemgusSynthFun synthFun, RelationTracker relations) {
            _theory = theory;
            _synthFun = synthFun;
            _relations = relations;
        }



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

        public InductiveConstraint ProcessConstraints(IEnumerable<SmtTerm> constraintTerms) {
            if (_synthFun.Rank.ReturnSort is not SemgusTermType termType) throw new InvalidDataException("Invalid synth-fun");

            var startSymbol = InferStartSymbol(termType, _synthFun.Grammar);

            return new(startSymbol, termType, constraintTerms.Select(ProcessBehaviorExample).ToList());
        }

        public static bool IsShapedLikeBehaviorExampleFor(SemgusSynthFun sf, SmtTerm constraintTerm) =>
            constraintTerm is SmtFunctionApplication constraintAppl &&
            constraintAppl.Arguments.Count >= 1 &&
            constraintAppl.Arguments[0] is SmtFunctionApplication sfAppl &&
            sfAppl.Definition.StringName() == sf.Relation.StringName();
        
        public BehaviorExample ProcessBehaviorExample(SmtTerm constraintTerm) {
            var sfName = _synthFun.Relation.StringName();

            if(
                constraintTerm is not SmtFunctionApplication constraintAppl ||
                constraintAppl.Arguments.Count < 1 ||
                constraintAppl.Arguments[0] is not SmtFunctionApplication sfAppl ||
                sfAppl.Definition.StringName() != sfName ||
                !_relations.TryMatchName(constraintAppl.Definition.StringName(), out var rel)
            ) {
                throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [constant_expression...])");
            }

            List<object> values = new();
            List<RelationSlotInfo> varInfo = new();

            for (int i = 0; i < rel.Slots.Count; i++) {
                var slot = rel.Slots[i];
                if (slot.Label == RelationSlotLabel.Term) continue;

                try {
                    values.Add(_theory.EvalConstant(constraintAppl.Arguments[i]));
                } catch (ArgumentException) {
                    throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [constant_expression...])");
                }

                varInfo.Add(slot);
            }

            var block = values.ToArray();

            if(block.Length != rel.Slots.Count-1) {
                throw new NotSupportedException("Constraint must be of the form (semantic_relation_name synth_fun_name [constant_expression...])");
            }

            return new BehaviorExample(varInfo, block);
        }
    }
}
