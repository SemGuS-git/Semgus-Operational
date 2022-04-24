namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    using static Op;
    using static NormalizationFrames;

    internal static class LogicalBranchesReduced {
        public static IExpression Normalize(IExpression expr) {
            Stack<IFrame> stack = new();

            stack.Push(new Root(expr));

            while (stack.TryPeek(out var frame)) {
                if (frame.WorkQueue.TryDequeue(out var next)) {
                    switch (next) {
                        case Ternary tern when MightBeBoolean(tern.ValIf) && MightBeBoolean(tern.ValElse):
                            stack.Push(new TernaryFlatten(tern.Cond, tern.ValIf, tern.ValElse));
                            break;
                        case InfixOperation _in when _in.Op == And:
                            stack.Push(new Conjunct(_in.Operands));
                            break;
                        case InfixOperation _in when _in.Op == Or:
                            stack.Push(new Disjunct(_in.Operands));
                            break;
                        default:
                            frame.ResultList.Add(next);
                            break;
                    }
                } else {
                    var a = stack.Pop().Bake();
                    if (stack.TryPeek(out var parent)) {
                        parent.ResultList.Add(a);
                    } else {
                        return a;
                    }
                }
            }
            throw new Exception("Didn't return properly");
        }


        static bool MightBeBoolean(IExpression expr) => expr switch {
            InfixOperation oper => oper.Op.GetTypeId() == BitType.Id,
            UnaryOperation unop => unop.Op.GetTypeId() == BitType.Id,
            Literal lit when lit.Value > 1 || lit.Value < 0 => false,
            _ => true,
        };

    }
}