using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class LeftBranch : ScopeBase {
        public IExpression Cond { get; }

        private readonly IReadOnlyList<IStatement> _elseBody;

        public LeftBranch(IExpression cond, IReadOnlyList<IStatement> bodyLhs, IReadOnlyList<IStatement> bodyRhs) : base(bodyLhs) {
            Cond = cond;
            _elseBody = bodyRhs;
        }

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);

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