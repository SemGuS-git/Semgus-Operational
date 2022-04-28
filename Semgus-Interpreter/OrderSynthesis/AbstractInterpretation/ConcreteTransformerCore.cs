using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class ConcreteTransformerCore {
        public readonly ISmtLibExpression[] OutputVarExpressions;
        public readonly (int tuple_idx, int field_idx)[] IndexMap;
        public readonly HashSet<int> RequiredTupleIndices;

        public ConcreteTransformerCore(ISmtLibExpression[] output_var_expressions, (int tuple_idx, int field_idx)[] index_map, HashSet<int> required_tuple_indices) {
            this.OutputVarExpressions = output_var_expressions;
            this.IndexMap = index_map;
            this.RequiredTupleIndices = required_tuple_indices;
        }

        public ConcreteTransformer Hydrate(MuxTupleType resultType) => new(resultType, OutputVarExpressions, IndexMap);
    }


    /* This is what gets converted to a maybe-monotone Sketch function def.
     * - If this sem doesn't need any inputs, it's constant, so skip it
     * 
     * Steps:
     * - Take, as input: a map from tuple indices to Sketch tuple types, as well as a tuple type for the output
     * - For each input *that it uses*, invent a function argument
     * - Foreach field of the output struct,
     *   - Convert the corresponding output-variable expresssion
     *   - Assign that expression to the field
     */
}
