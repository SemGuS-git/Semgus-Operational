namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class StructDefinition : IStatement {
        public StructId Id { get; }
        public IReadOnlyList<VarId> Props { get; }

        public string? Comment { get; set; }

        public StructDefinition(StructId id, IReadOnlyList<VarId> props) {
            Id = id;
            Props = props;
        }
        public StructDefinition(StructId id, params VarId[] props) {
            Id = id;
            Props = props;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            if (Comment is not null) lineReceiver.Add($"// {Comment}");
            lineReceiver.Add($"struct {Id} {{");
            lineReceiver.IndentIn();
            foreach (var prop in Props) {
                lineReceiver.Add($"{prop.Type} {prop.Name};");
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }
}
