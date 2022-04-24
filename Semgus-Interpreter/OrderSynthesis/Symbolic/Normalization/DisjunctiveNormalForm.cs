using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    using static Op;

    internal static class DisjunctiveNormalForm {
        public static IExpression Normalize(IExpression root) {
            List<IExpression> disjuncts = new List<IExpression>();

            Queue<IExpression> workQueue = new();
            workQueue.Enqueue(root);


            while (workQueue.TryDequeue(out var next)) {
                switch (next) {
                    case InfixOperation _in when _in.Op == And:
                        var result = DistributeConjunction(_in.Operands);
                        if (result.IsA) {
                            foreach (var new_conjunction in result.A!) workQueue.Enqueue(new_conjunction);
                        } else {
                            disjuncts.Add(result.B!);
                        }
                        break;

                    case InfixOperation _in when _in.Op == Or:
                        foreach (var a in _in.Operands) workQueue.Enqueue(a);
                        break;

                    default:
                        disjuncts.Add(next);
                        break;
                }
            }

            return Or.Of(disjuncts);
        }

        class Either<TA, TB> {
            public TA? A { get; }
            public TB? B { get; }
            public bool IsA { get; }

            public Either(TA a) {
                this.A = a;
                this.IsA = true;
            }
            public Either(TB b) {
                this.B = b;
                this.IsA = false;
            }

        }

        static Either<IEnumerable<IExpression>, IExpression> DistributeConjunction(IReadOnlyList<IExpression> terms) {
            Queue<IExpression> conjuncts = new(terms);
            List<IReadOnlyList<IExpression>> mu = new();

            bool any = false;

            while (conjuncts.TryDequeue(out var next)) {
                switch (next) {
                    case InfixOperation _in when _in.Op == And:
                        foreach (var a in _in.Operands) conjuncts.Enqueue(a);
                        break;
                    case InfixOperation _in when _in.Op == Or:
                        any = true;
                        mu.Add(_in.Operands);
                        break;
                    default:
                        mu.Add(new[] { next });
                        break;
                }
            }

            if (any) {
                return new(IterationUtil.CartesianProduct(mu).Select(hot_array => And.Of(new List<IExpression>(hot_array))));
            } else {
                return new(And.Of(mu.Select(m => m.Single()).ToList()));
            }
        }
    }
}