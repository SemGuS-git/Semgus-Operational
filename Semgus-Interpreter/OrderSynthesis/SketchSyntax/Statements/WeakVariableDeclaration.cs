namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record WeakVariableDeclaration  (Identifier TypeId, Identifier Id, IExpression? Def = null)  : IStatement, IVariableInfo  {
        public override string ToString() => Def is null ? $"{TypeId} {Id}" : $"{TypeId} {Id} = {Def}";
        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString()+';');
    }
}
