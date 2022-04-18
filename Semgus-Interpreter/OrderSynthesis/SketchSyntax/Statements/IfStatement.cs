namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record IfStatement(IExpression Condition, IReadOnlyList<IStatement> BodyLhs, IReadOnlyList<IStatement> BodyRhs) : IStatement {
        public IfStatement(IExpression Condition, params IStatement[] body) : this(Condition, body.ToList(), Array.Empty<IStatement>()) { }
        public IfStatement(IExpression Condition, IReadOnlyList<IStatement> body) : this(Condition, body, Array.Empty<IStatement>()) { }

        public virtual bool Equals(IfStatement? other) => other is not null
            && Condition.Equals(other.Condition)
            && BodyLhs.SequenceEqual(other.BodyLhs)
            && BodyRhs.SequenceEqual(other.BodyRhs);

        public override string ToString() => this.PrettyPrint(true);

        public void WriteInto(ILineReceiver lineReceiver) {
            switch(BodyLhs.Count) {
                case 0:
                    lineReceiver.Add($"if({Condition}) {{ }}");
                    break;
                case 1:
                    lineReceiver.Add($"if({Condition}) ");
                    BodyLhs[0].WriteInto(lineReceiver);
                    break;
                default:
                    lineReceiver.Add($"if({Condition}) {{");
                    lineReceiver.IndentIn();
                    foreach (var stmt in BodyLhs) {
                        stmt.WriteInto(lineReceiver);
                    }
                    lineReceiver.IndentOut();
                    lineReceiver.Add("}");
                    break;

            }
            switch (BodyRhs.Count) {
                case 0:
                    break;
                case 1:
                    lineReceiver.Add("else ");
                    BodyRhs[0].WriteInto(lineReceiver);
                    break;
                default:
                    lineReceiver.Add("else {");
                    lineReceiver.IndentIn();
                    foreach (var stmt in BodyRhs) {
                        stmt.WriteInto(lineReceiver);
                    }
                    lineReceiver.IndentOut();
                    lineReceiver.Add("}");
                    break;
            }
        }
    }
}
