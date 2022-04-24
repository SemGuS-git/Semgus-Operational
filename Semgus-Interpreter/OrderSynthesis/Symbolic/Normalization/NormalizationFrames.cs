using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    using static Op;

    internal static class NormalizationFrames {
        public interface IFrame {
            public Queue<IExpression> WorkQueue { get; }
            public List<IExpression> ResultList { get; }
            public IExpression Bake();

        }
        public abstract class Base : IFrame {
            public Queue<IExpression> WorkQueue { get; }
            public List<IExpression> ResultList { get; } = new();

            public Base(IEnumerable<IExpression> terms) {
                WorkQueue = new(terms);
            }

            public Base(params IExpression[] terms) {
                WorkQueue = new(terms);
            }

            public abstract IExpression Bake();
        }
        public class Root : Base {
            public Root(IExpression term) : base(term) { }
            public override IExpression Bake() {
                return ResultList.Single();
            }
        }
        public class Conjunct : Base {
            public Conjunct(IEnumerable<IExpression> terms) : base(terms) { }

            public override IExpression Bake() {

                return And.Of(ResultList);
            }
        }
        public class Disjunct : Base {
            public Disjunct(IEnumerable<IExpression> terms) : base(terms) { }
            public override IExpression Bake() {
                return Or.Of(ResultList);
            }
        }
        public class TernaryFlatten : Base {
            public TernaryFlatten(IExpression cond, IExpression left, IExpression right) : base() {
                while (cond is UnaryOperation _un && _un.Op == UnaryOp.Not) {
                    cond = _un.Operand;
                    (right, left) = (left, right);
                }
                WorkQueue.Enqueue(cond);
                WorkQueue.Enqueue(left);
                WorkQueue.Enqueue(right);
            }

            public override IExpression Bake() {
                Debug.Assert(ResultList.Count == 3);

                var cond = ResultList[0];
                var left = ResultList[1];
                var right = ResultList[2];

                if (cond.Equals(left)) {
                    return Or.Of(cond, right);
                } else if (cond.Equals(right)) {
                    return And.Of(cond, left);
                } else {
                    return Or.Of(And.Of(cond, left), right);
                }
            }
        }
    }
}