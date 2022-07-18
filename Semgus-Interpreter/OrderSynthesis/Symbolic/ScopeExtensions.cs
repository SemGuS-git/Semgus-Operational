namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal static class ScopeExtensions {

        public static void ReceiveExpression(this IScope frame, IExpression expr) {
            if (frame.PendingStack.TryPeek(out var pending)) {
                // If there is a pending statement which is waiting for an expression,
                // pass the expression to it
                pending.AddExpression(expr);
            }
            // otherwise discard the expression
        }
    }
}
