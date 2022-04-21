using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal static class SymbolicInterpreter {
        public record Result(IExpression ReturnValue, IReadOnlyDictionary<Identifier, IExpression> RefVariables, IReadOnlyDictionary<Identifier, IExpression> Globals);

        public static Result Evaluate(FunctionDefinition function, params FunctionDefinition[] enterableFunctions) => Evaluate(function, enterableFunctions.ToDictionary(f => f.Id));

        public static Result Evaluate(FunctionDefinition function, IReadOnlyDictionary<Identifier, FunctionDefinition> functionMap) {
            ScopeStack stack = new(functionMap);
            var root = new RootScope(function);
            stack.Push(root);

            while (stack.TryPeek(out var frame)) {
                if (frame.PendingStack.TryPeek(out var pending)) {
                    if (pending.RawExpressions.MoveNext()) {
                        ProcessExpression(stack, frame, pending.RawExpressions.Current);
                    } else {
                        frame.PendingStack.Pop().Finalize(stack, frame);
                    }
                } else {
                    if (frame.Enumerator.MoveNext()) {
                        frame.PendingStack.Push(ExpressionScope.From(frame.Enumerator.Current));
                    } else {
                        stack.Pop().OnPop(stack);
                    }
                }
            }

            return root.Result;
        }


        static void ProcessExpression(ScopeStack stack, IScope frame, IExpression expr) {
            switch (expr) {
                case Hole or Literal or Empty:
                    frame.ReceiveExpression(expr);
                    break;
                case Ternary _tern:
                    frame.PendingStack.Push(new(_tern, _tern.Cond, _tern.ValIf, _tern.ValElse));
                    break;
                case UnaryOperation _unary:
                    frame.PendingStack.Push(new(_unary, _unary.Operand));
                    break;
                case InfixOperation _infix:
                    frame.PendingStack.Push(new(_infix, _infix.Operands));
                    break;
                case FunctionEval _call:
                    frame.PendingStack.Push(new(_call, _call.Args));
                    break;
                case VariableRef _ref:
                    frame.ReceiveExpression(stack.Resolve(_ref.TargetId));
                    break;
                case StructNew _new:
                    stack.Push(new StructNewScope(_new));
                    break;
                case PropertyAccess _prop:
                    frame.PendingStack.Push(new(_prop, _prop.Expr));
                    break;
                default: throw new NotSupportedException();
            }
        }
    }
}