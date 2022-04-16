namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record VariableDeclaration(Variable Var, IExpression? Def = null) : IStatement {
        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString()+';');
        public override string ToString() => Def is null ? $"{Var.Type} {Var.Id}" : $"{Var.Type} {Var.Id} = {Def}";
    }
}
