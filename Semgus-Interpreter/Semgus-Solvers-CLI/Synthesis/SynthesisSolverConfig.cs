using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public record SynthesisSolverConfig(
         string MethodName,
         TimeSpan? Timeout,
         IReadOnlyDictionary<string, object> Params
        ) {

        private static SynthesisSolverConfig _default;
        public static SynthesisSolverConfig Default => _default ??= new("bottom_up", TimeSpan.FromMinutes(10), new Dictionary<string, object> { { "cost_function", "size" } });

        public static SynthesisSolverConfig FromToml(TomlTable table) => new(
            MethodName: table.GetValue<string>("method", required: true),
            Timeout: table.TryGetValue<double>("timeout", Convert.ToDouble, out var timeoutVal) ? TimeSpan.FromSeconds(timeoutVal) : null,
            Params: table.GetDictionary("params", required: false)
        );
    }
}