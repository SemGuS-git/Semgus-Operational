using Semgus.Operational;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class ConcreteTransformer {
        readonly int out_tuple_size;
        readonly MuxTupleType out_tuple_type;

        readonly ISmtLibExpression[] each_prop_expr;
        readonly int var_count;
        readonly (int tuple_idx, int field_idx)[] var_index_map;

        public ConcreteTransformer(MuxTupleType out_tuple_type, ISmtLibExpression[] each_prop_expr, (int tuple_idx, int field_idx)[] input_var_map) {
            this.out_tuple_type = out_tuple_type;
            this.out_tuple_size = out_tuple_type.Size;
            this.each_prop_expr = each_prop_expr;
            this.var_index_map = input_var_map;
            this.var_count = input_var_map.Length;
        }

        public MuxTuple Evaluate(List<MuxTuple> inputs) {
            object[] input_var_values = new object[var_count];

            for (int i = 0; i < var_count; i++) {
                var (tuple_idx, field_idx) = var_index_map[i];
                if (tuple_idx < 0) continue; // indicates output variable
                input_var_values[i] = inputs[tuple_idx].Values[field_idx];
            }

            EvaluationContext context = new(
                Array.Empty<IDSLSyntaxNode>(),
                input_var_values
            );

            object[] result = new object[out_tuple_type.Size];
            for (int i = 0; i < out_tuple_type.Size; i++) {
                result[i] = each_prop_expr[i].Evaluate(context);
            }

            return new(out_tuple_type, result);
        }
    }
}
