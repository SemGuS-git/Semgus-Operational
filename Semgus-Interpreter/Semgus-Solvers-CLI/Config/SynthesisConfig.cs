using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public record SynthesisConfig(
         string MethodName,
         TimeSpan? Timeout,
         IReadOnlyDictionary<string, object> Params
        ) {

        private static SynthesisConfig _default;
        public static SynthesisConfig Default => _default ??= new("bottom_up", TimeSpan.FromMinutes(10), new Dictionary<string, object> { { "cost_function", "size" }, { "reductions", new[] { "observational_equivalence" } } });

        public static SynthesisConfig FromToml(TomlTable table) => new(
            MethodName: table.GetValue<string>("method", required: true),
            Timeout: table.TryGetValue<double>("timeout", Convert.ToDouble, out var timeoutVal) ? TimeSpan.FromSeconds(timeoutVal) : null,
            Params: table.GetDictionary("params", required: false)
        );
    }
}