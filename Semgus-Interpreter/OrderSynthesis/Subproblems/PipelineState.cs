using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record PipelineState(
        PipelineState.Step Reached,
        IReadOnlyDictionary<Identifier, IType> TypeMap,
        IReadOnlyList<StructType> Structs,
        IReadOnlyList<FunctionDefinition>? Comparisons,
        IReadOnlyList<MonotoneLabeling>? MonotoneFunctions,
        IReadOnlyList<MonotoneLabeling>? AllMonotonicities,
        IReadOnlyList<LatticeDefs>? Lattices
    ) {
        public enum Step {
            Monotonicity,
            OrderExpansion,
            Simplification,
            Initial,
            Lattice,
        }
        public PipelineState(Step reached, IReadOnlyDictionary<Identifier, IType> typeMap, IReadOnlyList<StructType> structs) : this(reached, typeMap, structs, null, null, null, null) { }
    }
}
