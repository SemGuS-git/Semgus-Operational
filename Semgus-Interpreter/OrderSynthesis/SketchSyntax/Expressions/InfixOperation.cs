using System.Text;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record InfixOperation  (Op Op, IReadOnlyList<IExpression> Operands)  : IExpression  {
        public InfixOperation(Op op, params IExpression[] operands) : this(op, operands.ToList()) { }

        public override string ToString() {
            StringBuilder sb = new();

            for (int i = 0; i < Operands.Count; i++) {
                if (i > 0) {
                    sb.Append(' ');
                    sb.Append(Op.Str());
                    sb.Append(' ');
                }

                var e = Operands[i];

                if (e is Ternary || (e is InfixOperation inner && !inner.Op.HasLowerPrecedenceThan(Op))) {
                    sb.Append('(');
                    sb.Append(e);
                    sb.Append(')');
                } else {
                    sb.Append(e);
                }
            }
            return sb.ToString();
        }

        public virtual bool Equals(InfixOperation? other) => other is not null && Op.Equals(other.Op) && Operands.SequenceEqual(other.Operands);

        //private static bool ShouldParenthesize(Op outer, Op inner, int index) => outer switch {
        //    Op.Minus => OutType(inner) is BitType || !(index == 0 || inner == Op.Times),
        //    Op.Times => inner != Op.Times,
        //    Op.Plus => OutType(inner) is BitType,
        //    Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq => OutType(inner) is BitType,
        //    Op.And => inner == Op.Or,
        //    Op.Or => inner == Op.And,
        //    _ => throw new ArgumentOutOfRangeException(),
        //};

        //public static IType OutType(Op op) => op switch {
        //    Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq or Op.Or or Op.And => BitType.Instance,
        //    Op.Plus or Op.Minus or Op.Times => IntType.Instance,
        //    _ => throw new ArgumentOutOfRangeException(),
        //};

        public class Builder {
            public Op Op { get; }

            private readonly List<IExpression> _terms = new();
            public IReadOnlyList<IExpression> Terms => _terms;

            public Builder(Op op, IExpression head) {
                Op = op;
                _terms = new() { head };
            }

            public Builder Add(IExpression operand) {
                _terms.Add(operand);
                return this;
            }

            public InfixOperation GetValue() {
                if (_terms.Count < 2) throw new InvalidOperationException();
                return new(Op, _terms);
            }
        }


        public static IExpression GroupOperators(IExpression head, IEnumerable<(Op,IExpression)> tail) {
            var en = tail.GetEnumerator();

            var stack = new Stack<Builder>();

            var pending = head;

            while (en.MoveNext()) {
                var now = pending;
                (var op, pending) = en.Current;

                if (stack.TryPeek(out var stackTop)) {
                    if (op == stackTop.Op) {
                        stackTop.Add(now);
                        continue;
                    }

                    while (stackTop.Op.HasLowerPrecedenceThan(op)) {
                        now = stack.Pop().Add(now).GetValue();
                        if (!stack.TryPeek(out stackTop)) break;
                    }
                }

                stack.Push(new(op, now));
            }

            while(stack.TryPop(out var next)) {
                pending = next.Add(pending).GetValue();
            }

            return pending;
        }
    }
}
