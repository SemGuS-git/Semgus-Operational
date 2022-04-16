namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record RepeatStatement(IExpression Condition, IReadOnlyList<IStatement> Body) : IStatement {
        public RepeatStatement(IExpression condition, params IStatement[] body) : this(condition, body.ToList()) { }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"repeat({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }

        public virtual bool Equals(RepeatStatement? other) => other is not null && Condition.Equals(other.Condition) && Body.SequenceEqual(other.Body);

        public override string ToString() => this.PrettyPrint(true);
    }
}
