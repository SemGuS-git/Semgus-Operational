using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {

    public record InterpreterTask {
        public IReadOnlyList<string> Files { get; init; }
        public IReadOnlyList<string> Programs { get; init; }
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Examples { get; init; }
        public int RunCount { get; init; }
        public int MaxDepth { get; init; }

        public static InterpreterTask FromToml(TomlTable table) => new() {
            Files = table.GetAtomList<string>("files", required: true),
            Programs = table.GetAtomList<string>("programs", required: true),
            Examples = table.GetListOfObjects("examples", required: true),
            RunCount = table.GetValueOrDefault("run_count", Convert.ToInt32, 1),
            MaxDepth = table.GetValueOrDefault("max_depth", Convert.ToInt32, int.MaxValue),
        };
    }
}