namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class RepeatStatement : IStatement {
        public IExpression Condition { get; }
        public IReadOnlyList<IStatement> Body { get; }

        public RepeatStatement(IExpression condition, IReadOnlyList<IStatement> body) {
            Condition = condition;
            Body = body;
        }

        public RepeatStatement(IExpression condition, params IStatement[] body) {
            Condition = condition;
            Body = body;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"repeat({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }
}
