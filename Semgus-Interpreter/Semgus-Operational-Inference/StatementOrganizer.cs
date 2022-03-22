using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Operational {
    public class StatementOrganizer {
        class StepLexComparer : IComparer<IInterpretationStep> {
            public static StepLexComparer Instance { get; } = new StepLexComparer();
            private StepLexComparer() { }

            public int Compare(IInterpretationStep? x, IInterpretationStep? y) {
                if (x is null || y is null) throw new NullReferenceException();

                var baseline = GetIntCode(x).CompareTo(GetIntCode(y));
                if (baseline != 0) return baseline;
                return (x, y) switch {
                    (AssignmentFromLocalFormula ax, AssignmentFromLocalFormula ay) => ax.ResultVar.Index.CompareTo(ay.ResultVar.Index),
                    (TermEvaluation ex, TermEvaluation ey) => ex.Term.Index.CompareTo(ey.Term.Index),
                    _ => 0,
                };
            }

            private static int GetIntCode(IInterpretationStep step) => step switch {
                ConditionalAssertion => 0,
                AssignmentFromLocalFormula => 1,
                TermEvaluation => 2,
                _ => throw new NotImplementedException(),
            };
        }

        private readonly List<TermEvaluation> _termEvaluations = new();
        private readonly List<AmbiguousVariableEquality> _varEquals = new();
        private readonly List<ConditionalAssertion> _conditionalAssertions = new();

        internal void Add(TermEvaluation clause) => _termEvaluations.Add(clause);
        internal void Add(AmbiguousVariableEquality clause) => _varEquals.Add(clause);
        internal void Add(ConditionalAssertion clause) => _conditionalAssertions.Add(clause);

        public IReadOnlyList<IInterpretationStep> Resolve(LocalScopeVariables variables) {
            var settersByChildTerm = new Dictionary<string, TermEvaluation>();
            var settersByEquality = new Dictionary<string, AssignmentFromLocalFormula>();
            var newConditionalAssertions = new List<ConditionalAssertion>();

            //- For each inner term evaluation E, for each output variable x of E,
            //  - If x already has a setter, halt with error.
            //  - Else designate E as the setter of x.
            foreach (var e in _termEvaluations) {
                foreach (var v in e.OutputVariables) {
                    var x = v.Name;
                    if (settersByChildTerm.TryGetValue(x, out var e2)) {
                        throw new Exception($"Setter for {x} is ambiguous between {e} and {e2}");
                    }
                    settersByChildTerm.Add(x, e);
                }
            }

            // - For each variable equality E with possible assignee x,
            //   - If x is already set by an inner term eval, convert E to a conditional and add it to that list.
            //   - Else if x is already set by an equality, halt with error.
            //   - Else convert E to an assignment, add it to that list, and designate it as the setter of x.
            foreach (var e in _varEquals) {
                var x = e.LhsVariable.Name;
                if (settersByChildTerm.ContainsKey(x)) {
                    newConditionalAssertions.Add(e.AsConditional());
                } else if (settersByEquality.TryGetValue(x, out var e2)) {
                    throw new Exception($"Setter for {x} is ambiguous between {e} and {e2}");
                } else {
                    var setter = e.AsAssigment();
                    settersByEquality.Add(x, setter);
                }
            }

            // Validation logic
            foreach (var v in variables.Variables) {
                switch (v.Usage) {
                    case VariableUsage.Input:
                        // Check that no inputs are assigned
                        if (settersByChildTerm.TryGetValue(v.Name, out var k1)) throw new InvalidOperationException($"Attempting to assign a value to input variable {v.Name} (in step {k1}); this is not permitted");
                        if (settersByEquality.TryGetValue(v.Name, out var k2)) throw new InvalidOperationException($"Attempting to assign a value to input variable {v.Name} (in step {k1}); this is not permitted");
                        break;
                    case VariableUsage.Output:
                    case VariableUsage.Auxiliary:
                        // Check that all outputs and aux vars are assigned
                        if (!(settersByChildTerm.ContainsKey(v.Name) || settersByEquality.ContainsKey(v.Name))) throw new ArgumentException($"Variable {v.Name} is referenced but not defined");
                        break;
                }
            }

            var graph = new DependencyGraph<IInterpretationStep>();

            IReadOnlyCollection<IInterpretationStep> GetStepDeps(IEnumerable<VariableInfo> dependencies) {
                var stepDeps = new List<IInterpretationStep>();
                foreach (var v in dependencies) {
                    if (settersByChildTerm.TryGetValue(v.Name, out var e0)) {
                        stepDeps.Add(e0);
                    } else if (settersByEquality.TryGetValue(v.Name, out var e1)) {
                        stepDeps.Add(e1);
                    } else if (v.Usage != VariableUsage.Input) {
                        throw new Exception($"Required variable {v.Name} is {v.Usage}, but lacks a setter");
                    }
                }
                return stepDeps;
            }

            foreach (var step in _termEvaluations) graph.Add(step, GetStepDeps(step.InputVariables));
            foreach (var step in settersByEquality.Values) graph.Add(step, GetStepDeps(step.DependencyVariables));
            foreach (var step in newConditionalAssertions) graph.Add(step, GetStepDeps(step.DependencyVariables));
            foreach (var step in _conditionalAssertions) graph.Add(step, GetStepDeps(step.DependencyVariables));

            return graph.Sort(StepLexComparer.Instance);
        }

    }
}