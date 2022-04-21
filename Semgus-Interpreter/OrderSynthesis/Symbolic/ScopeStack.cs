using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class ScopeStack {
        private readonly Stack<IScope> _stack = new();
        private readonly IReadOnlyDictionary<Identifier, FunctionDefinition> _functionMap;

        public void Push(IScope scope) => _stack.Push(scope);

        public bool TryPeek(out IScope scope) => _stack.TryPeek(out scope);
        public bool TryPop(out IScope scope) => _stack.TryPop(out scope);
        public IScope Pop() => _stack.Pop();
        public IScope Peek() => _stack.Peek();

        public ScopeStack(IReadOnlyDictionary<Identifier,FunctionDefinition> functionMap) {
            this._functionMap = functionMap;
        }

        internal bool TryGetFunction(Identifier id, out FunctionDefinition function) => _functionMap.TryGetValue(id, out function);

        internal IExpression Resolve(Identifier id) {
            foreach(var frame in _stack) {
                if (frame.TryGetLocalValue(id, out var val)) return val;
            }
            return new VariableRef(id); // Treat this as a free variable
        }
    }

}