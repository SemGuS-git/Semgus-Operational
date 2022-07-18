using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class SingleExprEvalScope : IScope {
        public IEnumerator<IStatement> Enumerator => throw new InvalidOperationException();

        public Stack<ExpressionScope> PendingStack => throw new NotImplementedException();

        public void Assign(MiniParser.Identifier id, IExpression expression) => throw new InvalidOperationException();


        public void Declare(MiniParser.Identifier id, IExpression expression) => throw new InvalidOperationException();

        public IEnumerable<KeyValuePair<MiniParser.Identifier, IExpression>> GetSideEffectAssigns() => throw new InvalidOperationException();

        public void OnPop(ScopeStack stack) => throw new InvalidOperationException();
        public bool TryGetLocalValue(MiniParser.Identifier identifier, out IExpression expr) => throw new InvalidOperationException();
    }
    internal class LeftBranch : ScopeBase {
        public IExpression Cond { get; }

        private readonly IReadOnlyList<IStatement> _elseBody;

        public LeftBranch(IExpression cond, IReadOnlyList<IStatement> bodyLhs, IReadOnlyList<IStatement> bodyRhs) : base(bodyLhs) {
            Cond = cond;
            _elseBody = bodyRhs;
        }

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);
            
            // Unflatten locally assigned / defined struct values
            BakeAllStructVars();

            var parent = stack.Peek();

            if (_elseBody.Count > 0) {
                stack.Push(new RightBranch(this, _elseBody));
            } else {
                foreach (var kvp in GetSideEffectAssigns()) {
                    (var id, var left) = kvp;
                    IExpression right = stack.Resolve(id);
                    parent.Assign(id, new Ternary(Cond, left, right));
                }
            }
        }
    }
}