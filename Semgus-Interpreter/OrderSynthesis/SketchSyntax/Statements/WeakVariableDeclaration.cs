namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record WeakVariableDeclaration  (Identifier TypeId, Identifier Id, IExpression Def)  : IStatement, IVariableInfo  {
        public WeakVariableDeclaration(Identifier typeId, Identifier id) : this(typeId, id, Empty.Instance) { }

        public override string ToString() => Def is Empty ? $"{TypeId} {Id}" : $"{TypeId} {Id} = {Def}";
        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString()+';');
    }
}
