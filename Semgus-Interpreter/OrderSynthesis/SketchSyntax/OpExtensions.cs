namespace Semgus.OrderSynthesis.SketchSyntax {
    internal static class OpExtensions {
        public static string Str(this Op op) => op switch {
            Op.Eq => "==",
            Op.Neq => "!=",
            Op.Plus => "+",
            Op.Minus => "-",
            Op.Times => "*",
            Op.Or => "||",
            Op.And => "&&",
            Op.Lt => "<",
            Op.Leq => "<=",
            Op.Gt => ">",
            Op.Geq => ">=",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
