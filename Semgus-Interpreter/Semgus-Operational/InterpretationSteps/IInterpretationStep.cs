namespace Semgus.Interpretation {
    public interface IInterpretationStep {
        /// <summary>
        /// Print some imperative pseudocode to represent this statement.
        /// </summary>
        /// <returns></returns>
        string PrintCode();

        /// <summary>
        /// Execute the statement in the given evaluation context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>True if evaluation should continue; false otherwise.</returns>
        bool Execute(EvaluationContext context, InterpreterState state);
    }
}