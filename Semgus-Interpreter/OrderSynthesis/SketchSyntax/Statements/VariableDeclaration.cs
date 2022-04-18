namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record VariableDeclaration(Variable Var, IExpression Def) : IStatement {
        public VariableDeclaration(Variable var) : this(var, Empty.Instance) { }

        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString()+';');
        public override string ToString() => Def is Empty ? $"{Var.Type} {Var.Id}" : $"{Var.Type} {Var.Id} = {Def}";
    }
}
