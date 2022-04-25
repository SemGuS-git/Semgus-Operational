using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record StructDefinition  (Identifier Id, IReadOnlyList<Variable> Props)  : IStatement  {

        public string? Comment { get; set; }

        public StructDefinition(Identifier id, params Variable[] props) : this(id, props.ToList()) { }

        public void WriteInto(ILineReceiver lineReceiver) {
            if (Comment is not null) lineReceiver.Add($"// {Comment}");
            lineReceiver.Add($"struct {Id} {{");
            lineReceiver.IndentIn();
            foreach (var prop in Props) {
                lineReceiver.Add($"{prop.TypeId} {prop.Id};");
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }

        public virtual bool Equals(StructDefinition? other) => other is not null && Id.Equals(other.Id) && Props.SequenceEqual(other.Props);
        public override string ToString() => this.PrettyPrint(true);
    }
}
