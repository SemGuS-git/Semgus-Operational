namespace Semgus.Interpretation {
    public static class InterpreterHostExtensions {
        public static IReadOnlyDictionary<string, object> RunProgramReturningDict(this InterpreterHost interpreter, IDSLSyntaxNode node, IReadOnlyDictionary<string, object> input) {
            var providedInputNames = new HashSet<string>(input.Keys);
            foreach (var arg in node.ProductionRule.InputVariables) {
                if(!providedInputNames.Remove(arg.Name)) {
                    throw new ArgumentException($"Missing required input argument \"{arg.Name}\"");
                }
            }

            if (providedInputNames.Count > 0) {
                throw new ArgumentException($"Unexpected input argument(s) {string.Join(",", providedInputNames)}");
            }

            var result = interpreter.RunProgram(node, input);
            if (result.HasError) throw result.Error.InnerException;

            return node.LabelOutputs(result.Values);
        }
    }
}