using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record PropertyAccess  (IExpression Expr, Identifier Key)  : IExpression, ISettable  {
        public override string ToString() => $"{Expr}.{Key}";
    }
}
