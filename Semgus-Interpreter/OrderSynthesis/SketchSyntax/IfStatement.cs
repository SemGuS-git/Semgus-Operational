namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class IfStatement : IStatement {
        public IExpression Condition { get; }
        public IReadOnlyList<IStatement> Body { get; }

        public IfStatement(IExpression condition, IReadOnlyList<IStatement> body) {
            Condition = condition;
            Body = body;
        }

        public IfStatement(IExpression condition, params IStatement[] body) {
            Condition = condition;
            Body = body;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"if({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }
    internal class ElseIfStatement : IStatement {
        public IExpression Condition { get; }
        public IReadOnlyList<IStatement> Body { get; }

        public ElseIfStatement(IExpression condition, IReadOnlyList<IStatement> body) {
            Condition = condition;
            Body = body;
        }

        public ElseIfStatement(IExpression condition, params IStatement[] body) {
            Condition = condition;
            Body = body;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"else if({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }
}
