namespace Semgus.Interpretation {
    public class InterpreterHost {
        private readonly int _maxDepth;

        public InterpreterHost(int maxDepth) {
            this._maxDepth = maxDepth;
        }

        // Note: this does not check whether all provided inputs are consumed, or whether all provided inputs are correctly typed.
        // It will, however, throw an exception if an expected input is not provided.
        public InterpreterResult RunProgram(IDSLSyntaxNode node, IReadOnlyDictionary<string, object> input) {
            var block = new object[node.ProductionRule.MemorySize];
            foreach(var info in node.ProductionRule.InputVariables) {
                block[info.Index] = input[info.Name];
            }

            var state = new InterpreterState { maxDepth = _maxDepth };
            var evalContext = new EvaluationContext(node.AddressableTerms, block);

            node.ProductionRule.Interpret(evalContext, state);

            return state.HasError ? new InterpreterResult(state.Error) : new InterpreterResult(evalContext.Variables);
        }
    }
}