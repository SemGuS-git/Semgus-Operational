using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    using static NormalizationFrames;
    using static Op;

    internal static class NegationNormalForm {

        public static IExpression Normalize(IExpression root) {
            var stack = new Stack<IFrame>();

            static IExpression Flip(IExpression a) => new UnaryOperation(UnaryOp.Not, a);
            static IEnumerable<IExpression> FlipAll(IEnumerable<IExpression> ex) => ex.Select(Flip);

            stack.Push(new Root(root));

            void Invert(IFrame frame, IExpression term) {
                switch (term) {
                    case UnaryOperation double_invert when double_invert.Op == UnaryOp.Not:
                        frame.WorkQueue.Enqueue(double_invert.Operand);
                        break;
                    case InfixOperation _in when _in.Op == And:
                        stack.Push(new Disjunct(FlipAll(_in.Operands)));
                        break;
                    case InfixOperation _in when _in.Op == Or:
                        stack.Push(new Conjunct(FlipAll(_in.Operands)));
                        break;
                    default:
                        frame.ResultList.Add(UnaryOp.Not.Of(term));
                        break;
                }
            }

            while (stack.TryPeek(out var frame)) {
                if (frame.WorkQueue.TryDequeue(out var next)) {
                    switch (next) {
                        case UnaryOperation un when un.Op == UnaryOp.Not:
                            Invert(frame, un.Operand);
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
                    var res = stack.Pop().Bake();
                    if (stack.TryPeek(out var parent)) {
                        parent.ResultList.Add(res);
                    } else {
                        return res;
                    }
                }
            }

            throw new Exception("Reached end of loop without returning"); // should never happen
        }
    }
}