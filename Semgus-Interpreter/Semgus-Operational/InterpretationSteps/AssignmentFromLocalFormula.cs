namespace Semgus.Operational {
    /// <summary>
    /// Evaluate some formula (not containing child terms) and assign the result to a specific variable.
    /// </summary>
    public class AssignmentFromLocalFormula : IInterpretationStep {
        public ISmtLibExpression Expression { get; }
        public VariableInfo ResultVar { get; }
        public IReadOnlyCollection<VariableInfo> DependencyVariables { get; }

        public AssignmentFromLocalFormula(ISmtLibExpression expression, VariableInfo resultVar, IReadOnlyCollection<VariableInfo> dependencyVariables) {
            Expression = expression;
            ResultVar = resultVar;
            DependencyVariables = dependencyVariables;
        }

        //public bool Execute(EvaluationContext context, InterpreterState state) {
        //    var result = context.GetVariable(ResultVar.Name);
        //    try {
        //        result.SetValue(_expression.Evaluate(context));
        //    return true;
        //    } catch(Exception e) {
        //        state.FlagException(e,this,context);
        //        return false;
        //    }
        //}

        public bool Execute(EvaluationContext context, InterpreterState state) {
            try {
                context.Variables[ResultVar.Index] = Expression.Evaluate(context);
                return true;
            } catch (Exception e) {
                state.FlagException(e, this, context);
                return false;
            }
        }

        public string PrintCode() => $"{ResultVar.Name} := {Expression.PrettyPrint()}";

        public override string ToString() => PrintCode();
    }
}