namespace Semgus.Interpretation {
    public class ConditionalAssertion : IInterpretationStep {
        public ISmtLibExpression Expression { get; }
        public IReadOnlyCollection<VariableInfo> DependencyVariables { get; }

        public ConditionalAssertion(ISmtLibExpression expression, IReadOnlyCollection<VariableInfo> dependencyVariables) {
            Expression = expression;
            DependencyVariables = dependencyVariables;
        }

        public bool Execute(EvaluationContext context, InterpreterState state) {
            try {
                return (bool)Expression.Evaluate(context);
            } catch (Exception e) {
                state.FlagException(e, this, context);
                return false;
            }
        }

        public override string ToString() => PrintCode();

        public string PrintCode() => $"Assert {Expression.PrettyPrint()}";
    }
}