using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using static Sugar;

    internal class StructType : IType {
        public Identifier Id { get; }
        public string Name => Id.Name;

        public string? Comment { get; set; }

        public IReadOnlyList<Variable> Elements { get; }

        public Identifier CompareId { get; }
        public Identifier DisjunctId { get; }
        public Identifier EqId { get; }
        public Identifier NonEqId { get; }
        public Identifier BotTopValues { get; }

        public StructType(Identifier id, IReadOnlyList<Variable> elements) {
            this.Id = id;
            this.Elements = elements;

            CompareId = new($"compare_{id}");
            EqId = new($"eq_{id}");
            DisjunctId = new($"disjunct_{id}");
            NonEqId = new($"non_eq_{id}");
        }

        public override string ToString() => Id.ToString();
    }
}
