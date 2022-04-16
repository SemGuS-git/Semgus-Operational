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

        private static int GetPrecedence(this Op op) => op switch {
            Op.Times => 1,
            Op.Plus or Op.Minus => 2,
            Op.Lt or Op.Leq or Op.Gt or Op.Geq => 3,
            Op.Eq or Op.Neq => 4,
            Op.And => 5,
            Op.Or => 6,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public static bool HasLowerPrecedenceThan(this Op op, Op other) => op.GetPrecedence() < other.GetPrecedence();
    }
}
