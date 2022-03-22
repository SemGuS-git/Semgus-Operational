using Microsoft.Extensions.Logging;

namespace Semgus.Operational {
    public class InterpreterHost {
        public ILogger? Logger { get; set; }

        private readonly int _maxDepth;

        public InterpreterHost(int maxDepth) {
            this._maxDepth = maxDepth;
        }

        // Note: this does not check whether all provided inputs are consumed, or whether all provided inputs are correctly typed.
        // It will, however, throw an exception if an expected input is not provided.
        public InterpreterResult RunProgram(IDSLSyntaxNode node, IReadOnlyDictionary<string, object> input) {
            var block = new object[node.ProductionRule.MemorySize];
            foreach (var info in node.ProductionRule.InputVariables) {
                block[info.Index] = input[info.Name];
            }

            var state = new InterpreterState { maxDepth = _maxDepth };
            var evalContext = new EvaluationContext(node.AddressableTerms, block);

            node.ProductionRule.Interpret(evalContext, state);

            if (state.HasError) {
                Logger?.LogTrace(state.Error!.ToString());
                return new InterpreterResult(state.Error!);
            } else {
                return new InterpreterResult(evalContext.Variables);
            }
        }

        public InterpreterResult RunProgram(IDSLSyntaxNode node, object[] argValues) {
            var block = new object[node.ProductionRule.MemorySize];

            var n = node.ProductionRule.InputVariables.Count;
            if (argValues.Length < n) throw new Exception();

            for (int i = 0; i < n; i++) {
                var j = node.ProductionRule.InputVariables[i].Index;
                block[j] = argValues[j];
            }
            
            var state = new InterpreterState { maxDepth = _maxDepth };
            var evalContext = new EvaluationContext(node.AddressableTerms, block);

            node.ProductionRule.Interpret(evalContext, state);

            if (state.HasError) {
                Logger?.LogTrace(state.Error!.ToString());
                return new InterpreterResult(state.Error!);
            } else {
                return new InterpreterResult(evalContext.Variables);
            }
        }
    }
}