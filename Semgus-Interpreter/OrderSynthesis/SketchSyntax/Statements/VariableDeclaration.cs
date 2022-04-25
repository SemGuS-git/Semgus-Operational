using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record VariableDeclaration(Variable Variable, IExpression Def) : IStatement {
        public VariableDeclaration(Variable variable) : this(variable, Empty.Instance) { }

        public override string ToString() => Def is Empty ? Variable.ToString() : $"{Variable} = {Def}";
        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString() + ';');
    }
}
