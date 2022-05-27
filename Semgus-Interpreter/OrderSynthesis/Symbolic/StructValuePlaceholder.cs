using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class StructValuePlaceholder : IExpression {
        public Identifier Id { get; }
        public StructNew Source { get; }
        public IReadOnlyDictionary<Identifier, Identifier> FlatToPropMap;

        public StructValuePlaceholder(Identifier id, StructNew source, Dictionary<Identifier, Identifier> flat_to_prop_map) {
            this.Id = id;
            this.Source = source;
            this.FlatToPropMap = flat_to_prop_map;
        }
    }
}
