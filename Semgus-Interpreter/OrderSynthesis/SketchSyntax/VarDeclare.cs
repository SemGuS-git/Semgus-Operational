namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class VarDeclare : IStatement {
        public VarId Id { get; }
        public IExpression? Def { get; }

        public VarDeclare(VarId id, IExpression? def = null) {
            Id = id;
            Def = def;
        }

        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(Def is null ? $"{Id.Type} {Id.Name};" : $"{Id.Type} {Id.Name} = {Def};");
    }
}
