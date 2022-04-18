namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal static class ScopeExtensions {
        public static void ReceiveExpression(this IScope frame, IExpression expr) {
            if (frame.PendingStack.TryPeek(out var pending)) {
                pending.AddExpression(expr);
            }
            // otherwise discard this expression
        }
    }
}