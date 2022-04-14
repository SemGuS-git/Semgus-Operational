namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class NewExpression : IExpression {
        public IType Type { get; }
        public IReadOnlyList<Assignment> Args { get; }

        public NewExpression(IType type, IReadOnlyList<Assignment> args) {
            Type = type;
            Args = args;
        }

        public override string ToString() => $"new {Type}({string.Join(", ", Args)})";
    }
}
