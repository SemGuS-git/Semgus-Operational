using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {



    internal static class BitTernaryFlattener {

        public static IExpression Normalize(IExpression expr) {
            Stack<EF.IFrame> stack = new();

            stack.Push(new EF.Root(expr));


            while (stack.TryPeek(out var frame)) {
                if (frame.WorkQueue.TryDequeue(out var next)) {
                    switch (next) {
                        case Ternary tern when MightBeBoolean(tern.ValIf) && MightBeBoolean(tern.ValElse):
                            stack.Push(new EF.TernaryFlatten(tern.Cond, tern.ValIf, tern.ValElse));
                            break;
                        case InfixOperation _in when _in.Op == Op.And:
                            stack.Push(new EF.Conjunct(_in.Operands));
                            break;
                        case InfixOperation _in when _in.Op == Op.Or:
                            stack.Push(new EF.Disjunct(_in.Operands));
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

    internal static class DisjunctiveNormalForm {
        public static IExpression Normalize(IExpression root) {
            List<IExpression> disjuncts = new List<IExpression>();

            Queue<IExpression> workQueue = new();
            workQueue.Enqueue(root);


            while (workQueue.TryDequeue(out var next)) {
                switch (next) {
                    case InfixOperation _in when _in.Op == Op.And:
                        var result = DistributeConjunction(_in.Operands);
                        if (result.IsA) {
                            foreach (var new_conjunction in result.A!) workQueue.Enqueue(new_conjunction);
                        } else {
                            disjuncts.Add(result.B!);
                        }
                        break;
                    case InfixOperation _in when _in.Op == Op.Or:
                        foreach (var a in _in.Operands) workQueue.Enqueue(a);
                        break;
                    default:
                        disjuncts.Add(next);
                        break;
                }
            }

            return Op.Or.Of(disjuncts);
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
                    case InfixOperation _in when _in.Op == Op.And:
                        foreach (var a in _in.Operands) conjuncts.Enqueue(a);
                        break;
                    case InfixOperation _in when _in.Op == Op.Or:
                        any = true;
                        mu.Add(_in.Operands);
                        break;
                    default:
                        mu.Add(new[] { next });
                        break;
                }
            }

            if (any) {
                return new(IterationUtil.CartesianProduct(mu).Select(hot_array => Op.And.Of(new List<IExpression>(hot_array))));
            } else {
                return new(Op.And.Of(mu.Select(m => m.Single()).ToList()));
            }
        }
    }

    internal static class EF {
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
                
                return Op.And.Of(ResultList);
            }
        }
        public class Disjunct : Base {
            public Disjunct(IEnumerable<IExpression> terms) : base(terms) { }
            public override IExpression Bake() {
                return Op.Or.Of(ResultList);
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
                    return Op.Or.Of(cond, right);
                } else if (cond.Equals(right)) {
                    return Op.And.Of(cond, left);
                } else {
                    return Op.Or.Of(Op.And.Of(cond, left), right);
                }
            }
        }
    }

    internal static class NegationNormalForm {

        public static IExpression Normalize(IExpression root) {
            var stack = new Stack<EF.IFrame>();

            static IExpression Flip(IExpression a) => new UnaryOperation(UnaryOp.Not, a);
            static IEnumerable<IExpression> FlipAll(IEnumerable<IExpression> ex) => ex.Select(Flip);

            stack.Push(new EF.Root(root));

            void Invert(EF.IFrame frame, IExpression term) {
                switch (term) {
                    case UnaryOperation double_invert when double_invert.Op == UnaryOp.Not:
                        frame.WorkQueue.Enqueue(double_invert.Operand);
                        break;
                    case InfixOperation _in when _in.Op == Op.And:
                        stack.Push(new EF.Disjunct(FlipAll(_in.Operands)));
                        break;
                    case InfixOperation _in when _in.Op == Op.Or:
                        stack.Push(new EF.Conjunct(FlipAll(_in.Operands)));
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
                        case InfixOperation _in when _in.Op == Op.And:
                            stack.Push(new EF.Conjunct(_in.Operands));
                            break;
                        case InfixOperation _in when _in.Op == Op.Or:
                            stack.Push(new EF.Disjunct(_in.Operands));
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

            throw new Exception("hmm");
        }


        //public static InfixOperation ProcessArgs(InfixOperation oper) {
        //    if (!oper.Op.IsAssociative()) {
        //        return ;
        //    }

        //    List<IExpression> operands = new();
        //    foreach (var operand in oper.Operands) {
        //        if (operand is InfixOperation other_infix && other_infix.Op == oper.Op) {
        //            operands.AddRange(other_infix.Operands.Select(TernaryToLogical)));
        //        } else {
        //            operands.Add(TernaryToLogical(operand));
        //        }
        //    }

        //    return oper with { Operands = operands };
        //}


        //public static IExpression ToDNF(IExpression expr) => expr switch {
        //    InfixOperation oper when oper.Op == Op.And => DistributeConjunction(oper.Operands),
        //    InfixOperation oper when oper.Op == Op.Or => ProcessDisjunction(oper.Operands),
        //    UnaryOperation oper when oper.Op == UnaryOp.Not => ProcessNegation(oper.Operand),
        //    _ => expr,
        //};

        //private static object DistributeConjunction(IReadOnlyList<IExpression> operands) {
        //    List<IReadOnlyList<IExpression>> pieces = new();

        //    foreach (var a in operands) {
        //        switch (a) {
        //            case InfixOperation a_or when a_or.Op == Op.Or:
        //                pieces.Add(a_or.Operands);
        //                break;
        //            case InfixOperation a_and when a_and.Op == Op.And:
        //                pieces.AddRange(a_and.Operands.Select(c => new[] { c }));
        //                break;
        //            default:

        //        }
        //    }
        //    operands.Select(a => a switch {
        //    });

        //    // Agglomerate

        //    throw new NotImplementedException();
        //}

        //private static IExpression ProcessNegation(IExpression operand) => operand switch {
        //    InfixOperation oper when oper.Op == Op.And => Op.Or.Of(oper.Operands.Select(ProcessNegation).ToList()),
        //    InfixOperation oper when oper.Op == Op.Or => Op.And.Of(oper.Operands.Select(ProcessNegation).ToList()),
        //    UnaryOperation oper when oper.Op == UnaryOp.Not => ToDNF(oper.Operand),
        //    _ => UnaryOp.Not.Of(operand),
        //};


    }
}