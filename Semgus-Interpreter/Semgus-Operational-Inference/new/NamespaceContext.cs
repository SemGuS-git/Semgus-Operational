using Semgus.Interpretation;
using Semgus.Model.Smt.Terms;
using static Semgus.Model.SemgusChc;

namespace Semgus {
    internal class NamespaceContext {
        private readonly ITheoryImplementation _theory;

        private readonly RelationTracker _relations;
        private readonly LocalScopeTerms _terms;

        public LocalScopeVariables Variables { get; }

        public NamespaceContext(ITheoryImplementation theory, RelationTracker relBook, LocalScopeTerms terms, LocalScopeVariables variables) {
            this._theory = theory;
            this._relations = relBook;
            this._terms = terms;
            this.Variables = variables;
        }

        public TermEvaluation MakeTermEvaluation(SemanticRelation rel) {
            if (!_relations.TryMatch(rel, out var relInfo)) throw new InvalidDataException("Unable to match semantic relation signature");
            var slots = relInfo.Slots;

            TermVariableInfo? subject = null;

            List<(bool isOutput, VariableInfo info)> args = new();

            for (int i = 0; i < slots.Count; i++) {
                var arg = rel.Arguments[i];
                switch (slots[i].Label) {
                    case RelationSlotLabel.Term:
                        if (subject is not null) throw new NotSupportedException("Semantic relation of more than one term");
                        if (!_terms.TryMatch(arg, out subject)) throw new InvalidDataException("Unknown term variable identifier");
                        break;
                    case RelationSlotLabel.Input:
                    case RelationSlotLabel.Output:
                        if (!Variables.TryMatch(arg, out var info0)) throw new InvalidDataException("Unknown variable identifier");
                        args.Add((slots[i].Label == RelationSlotLabel.Output, info0));
                        break;
                }
            }

            if (subject is null) throw new NotSupportedException("Semantic relation with no term");

            // Validity check
            if (args.Any(tu => tu.isOutput && tu.info.Usage == VariableUsage.Input)) throw new NotSupportedException("Semantic relation binding a value to variable marked as input");

            return new(subject, args);
        }

        public bool MakeMaybeSetter(SmtFunctionApplication node, out AmbiguousVariableEquality? step) {
            if (
                node.Definition.IsEquality() &&
                node.Rank.Arity == 2 &&
                node.Arguments[0] is SmtVariable lhs &&
                Variables.TryMatch(lhs, out var lhs_info) &&
                lhs_info.Usage != VariableUsage.Input
            ) {
                if (!_theory.TryGetFunction(node.Definition, node.Rank, out var fn)) throw new KeyNotFoundException();
                var rhs_dep_map = new Dictionary<string, VariableInfo>();
                var rhs_expr = ToLocalExpression(node.Arguments[1], rhs_dep_map);

                var lhs_expr = new VariableEvalExpression(lhs_info);

                var full_expr = new FunctionCallExpression(fn, new[] { lhs_expr, rhs_expr });

                step = new(full_expr, rhs_expr, lhs_info, rhs_dep_map.Values.ToList());
                return true;
            } else {
                step = null;
                return false;
            }

        }

        public ConditionalAssertion MakeAssertion(SmtTerm node) {
            var depMap = new Dictionary<string, VariableInfo>();
            var expr = ToLocalExpression(node, depMap);
            return new(expr, depMap.Values.ToList());
        }

        public ISmtLibExpression ToLocalExpression(SmtTerm node, Dictionary<string, VariableInfo> depMap) {

            switch (node) {
                case SmtFunctionApplication fa:
                    if (!_theory.TryGetFunction(fa.Definition, fa.Rank, out var fn)) throw new KeyNotFoundException();

                    var args = new List<ISmtLibExpression>();
                    foreach (var raw_arg in fa.Arguments) {
                        args.Add(ToLocalExpression(raw_arg, depMap));
                    }

                    return new FunctionCallExpression(fn, args);
                case SmtLiteral lit:
                    return new LiteralExpression(lit.BoxedValue,lit.Sort);
                case SmtVariable v:
                    if (!Variables.TryMatch(v, out var info)) throw new KeyNotFoundException();
                    depMap.TryAdd(info.Name, info);
                    return new VariableEvalExpression(info);
                default:
                    throw new NotSupportedException($"Unexpected {node.GetType().Name} in local SMT expression");

            }
        }
    }
}
