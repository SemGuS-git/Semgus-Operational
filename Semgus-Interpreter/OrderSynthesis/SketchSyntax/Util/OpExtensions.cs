using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal static class OpExtensions {
        public static string Str(this UnaryOp op) => op switch {
            UnaryOp.Not => "!",
            UnaryOp.Minus => "-",

            _ => throw new ArgumentOutOfRangeException(),
        };
        public static Identifier GetTypeId(this UnaryOp op) => op switch {
            UnaryOp.Not => BitType.Id,
            UnaryOp.Minus => IntType.Id,

            _ => throw new ArgumentOutOfRangeException(),
        };

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
        public static Identifier GetTypeId(this Op op) => op switch {
            Op.Eq or
            Op.Neq or
            Op.Or or
            Op.And or
            Op.Lt or
            Op.Leq or
            Op.Gt or
            Op.Geq => BitType.Id,

            Op.Plus or
            Op.Minus or
            Op.Times => AnyType.Id,

            _ => throw new ArgumentOutOfRangeException(),
        };
        public static bool IsAssociative(this Op op) => op switch {
            Op.Or or
            Op.And or
            Op.Plus or
            Op.Times => true,
            _=>false,
        };

    }
}
