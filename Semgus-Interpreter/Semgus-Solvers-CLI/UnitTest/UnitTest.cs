using System;
using System.Collections.Generic;
using System.Linq;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public class UnitTest {
        public string Program { get; init; }
        public bool? SynthFunSolution { get; init; }
        public int MaxDepth { get; init; }
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Examples { get; init; }
        public IReadOnlyList<IReadOnlyDictionary<string,object>> ErrorExamples { get; init; }

        public static UnitTest FromToml(TomlTable table) => new() {
            Program = table.GetValue<string>("program", required: true),
            SynthFunSolution = table.TryGetValue<bool>("synth_fun_solution", out var bval) ? bval : null,
            MaxDepth = table.GetValueOrDefault("max_depth", Convert.ToInt32, int.MaxValue),
            Examples = table.GetListOfObjects("examples", required: false),
            ErrorExamples = table.GetListOfObjects("error_examples", required: false),
        };

        private bool IsValid(out string issue) {
            if (!SynthFunSolution.HasValue && (Examples?.Count ?? 0) == 0) {
                issue = "Unit test program must either set synth_fun_solution or have a list of examples";
                return false;
            }

            issue = default;
            return true;
        }
    }
}