namespace Semgus.OrderSynthesis.SketchSyntax {
    internal abstract record CondStatementBase(IExpression Condition, IReadOnlyList<IStatement> Body) : IStatement {

        protected abstract string Keyword { get; }

        public CondStatementBase(IExpression condition, params IStatement[] body) : this(condition, body.ToList()) { }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"{Keyword}({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }

    internal record IfStatement(IExpression Condition, IReadOnlyList<IStatement> Body) : CondStatementBase(Condition,Body) {
        protected override string Keyword => "if";

        public IfStatement(IExpression Condition, params IStatement[] body) : this(Condition, body.ToList()) { }

        public virtual bool Equals(IfStatement? other) => other is not null && Condition.Equals(other.Condition) && Body.SequenceEqual(other.Body);

        public override string ToString() => this.PrettyPrint(true);
    }

    internal record ElseIfStatement(IExpression Condition, IReadOnlyList<IStatement> Body) : CondStatementBase(Condition, Body) {
        protected override string Keyword => "else if";
        public ElseIfStatement(IExpression Condition, params IStatement[] body) : this(Condition, body.ToList()) { }

        public virtual bool Equals(ElseIfStatement? other) => other is not null && Condition.Equals(other.Condition) && Body.SequenceEqual(other.Body);
        public override string ToString() => this.PrettyPrint(true);
    }

    internal record ElseStatement(IReadOnlyList<IStatement> Body) : IStatement {

        public ElseStatement(params IStatement[] body) : this(body.ToList()) { }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add("else {");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }

        public virtual bool Equals(ElseStatement? other) => other is not null && Body.SequenceEqual(other.Body);

        public override string ToString() => this.PrettyPrint(true);
    }
}
