namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class FunctionEval : IExpression {
        public FunctionId Id { get; }
        public IReadOnlyList<IExpression> Args { get; }

        public FunctionEval(FunctionId id, IReadOnlyList<IExpression> args) {
            Id = id;
            Args = args;
        }

        public FunctionEval(FunctionId id, params IExpression[] args) {
            Id = id;
            Args = args;
        }

        public override string ToString() => $"{Id}({string.Join(", ", Args)})";
    }
}
