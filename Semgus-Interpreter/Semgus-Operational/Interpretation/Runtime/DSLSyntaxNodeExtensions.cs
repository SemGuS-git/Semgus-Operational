namespace Semgus.Operational {
    public static class DSLSyntaxNodeExtensions {

        // TODO: these methods assume that input and output variables are unique. Rethink?

        public static IReadOnlyDictionary<string, object> LabelInputs(this IDSLSyntaxNode node, IReadOnlyList<object> rawValues) =>
            node.ProductionRule.InputVariables.ToDictionary(info => info.Name, info => rawValues[info.Index]);

        public static IReadOnlyDictionary<string, object> LabelOutputs(this IDSLSyntaxNode node, IReadOnlyList<object> rawValues) => 
            node.ProductionRule.OutputVariables.ToDictionary(info => info.Name, info => rawValues[info.Index]);

        public static IReadOnlyList<object> ExtractOutputValues(this IDSLSyntaxNode node, IReadOnlyList<object> values) {
            var o = node.ProductionRule.OutputVariables;
            var n = o.Count;
            var a = new object[n];
            for(int i = 0; i< n; i++) {
                a[i] = values[o[i].Index];
            }
            return a;
        }

        public static (IReadOnlyDictionary<string,object> input, IReadOnlyDictionary<string,object> output) SplitInputOutput(this IDSLSyntaxNode node, IReadOnlyDictionary<string,object> example) {
            var input = new Dictionary<string, object>();
            var output = new Dictionary<string, object>();
            var remaining = new HashSet<string>(example.Keys);

            foreach(var arg in node.ProductionRule.InputVariables) {
                var name = arg.Name;
                if (example.TryGetValue(name, out var value)) {
                    remaining.Remove(name);
                    input.Add(name, value);
                } else {
                    throw new ArgumentException($"Missing input variable {{{arg.Name}}}");
                }
            }
            foreach(var arg in node.ProductionRule.OutputVariables) {
                var name = arg.Name;
                if(example.TryGetValue(name,out var value)) {
                    remaining.Remove(name);
                    output.Add(name, value);
                }
            }

            if(remaining.Count>0) {
                throw new ArgumentException($"Provided variables {{{string.Join(", ", remaining)}}} were not found in term signature");
            }

            return (input, output);
        }
    }
}