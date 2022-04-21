using Semgus.MiniParser;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class RightBranch : ScopeBase {
        private readonly LeftBranch _left;

        public RightBranch(LeftBranch left, IEnumerable<IStatement> body) : base(body) {
            _left = left;
        }

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);

            var parent = stack.Peek();
            var workList = new Dictionary<Identifier, IExpression>(GetSideEffectAssigns());

            foreach (var kvp in _left.GetSideEffectAssigns()) {
                (var id, var left) = kvp;
                IExpression right = workList.Remove(id, out var t) ? t : stack.Resolve(id);
                parent.Assign(id, new Ternary(_left.Cond, left, right));
            }
            foreach (var kvp in workList) {
                (var id, var right) = kvp;
                IExpression left = stack.Resolve(id);
                parent.Assign(id, new Ternary(_left.Cond, left, right));
            }
        }
    }
}