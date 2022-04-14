namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class Assignment : IStatement, IExpression {
        public ISettable Subject { get; }
        public IExpression Value { get; }

        public Assignment(ISettable subject, IExpression value) {
            Subject = subject;
            Value = value;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"{Subject} = {Value};");
        }

        public override string ToString() => $"{Subject} = {Value}";
    }
}
