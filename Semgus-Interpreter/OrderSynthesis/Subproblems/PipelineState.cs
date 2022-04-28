using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record PipelineState(
        PipelineState.Step Reached,
        IReadOnlyDictionary<Identifier, StructType> StructTypeMap,
        IReadOnlyList<StructType> StructTypeList,
        IReadOnlyList<FunctionDefinition>? Comparisons,
        IReadOnlyList<MonotoneLabeling>? LabeledTransformers,
        IReadOnlyList<LatticeDefs>? Lattices
    ) {
        public enum Step {
            Monotonicity,
            OrderExpansion,
            Simplification,
            Initial,
            Lattice,
        }
        public PipelineState(
            Step reached,
            IReadOnlyDictionary<Identifier, StructType> structTypeMap,
            IReadOnlyList<StructType> structTypeList
        ) : this(reached, structTypeMap, structTypeList, null, null, null) { }

    }
}
