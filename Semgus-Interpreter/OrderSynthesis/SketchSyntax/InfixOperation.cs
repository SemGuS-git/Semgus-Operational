using System.Text;



namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class InfixOperation : IExpression {
        public Op Op { get; }
        public IReadOnlyList<IExpression> Operands { get; }

        public InfixOperation(Op op, IReadOnlyList<IExpression> operands) {
            Op = op;
            Operands = operands;
        }
        public InfixOperation(Op op, params IExpression[] operands) {
            Op = op;
            Operands = operands;
        }

        public override string ToString() {
            StringBuilder sb = new();

            for (int i = 0; i < Operands.Count; i++) {
                if (i > 0) {
                    sb.Append(' ');
                    sb.Append(Op.Str());
                    sb.Append(' ');
                }

                var e = Operands[i];

                if (e is InfixOperation inner && ShouldParenthesize(Op,inner.Op, i)) {
                    sb.Append('(');
                    sb.Append(inner);
                    sb.Append(')');
                } else {
                    sb.Append(e);
                }
            }
            return sb.ToString();
        }
        private static bool ShouldParenthesize(Op outer, Op inner, int index) => outer switch {
            Op.Minus => OutType(inner) is BitType || !(index == 0 || inner == Op.Times),
            Op.Times => inner != Op.Times,
            Op.Plus => OutType(inner) is BitType,
            Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq => OutType(inner) is BitType,
            Op.And => inner == Op.Or,
            Op.Or => inner == Op.And,
            _ => throw new ArgumentOutOfRangeException(),
        };
        public static IType OutType(Op op) => op switch {
            Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq or Op.Or or Op.And => BitType.Instance,
            Op.Plus or Op.Minus or Op.Times => IntType.Instance,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
