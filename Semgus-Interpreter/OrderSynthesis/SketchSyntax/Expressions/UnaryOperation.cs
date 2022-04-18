namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record UnaryOperation (UnaryOp Op, IExpression Operand) : IExpression {
        public override string ToString() => Operand switch {
            UnaryOperation or InfixOperation or Ternary or StructNew => $"{Op.Str()}({Operand})",
            _ => $"{Op.Str()}{Operand}"
        };
    }
}
