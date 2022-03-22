using System.Collections.Generic;

namespace Semgus.Operational {
    public record AmbiguousVariableEquality(
        ISmtLibExpression FullExpression,
        ISmtLibExpression RhsExpression,
        VariableInfo LhsVariable, 
        IReadOnlyCollection<VariableInfo> RhsVariables
    ) {
        public AssignmentFromLocalFormula AsAssigment() => new(RhsExpression, LhsVariable, RhsVariables);
        public ConditionalAssertion AsConditional() => new(FullExpression, new HashSet<VariableInfo>(RhsVariables) { LhsVariable });

        public override string ToString() => $"Somehow {FullExpression.PrettyPrint()}";
    }
}