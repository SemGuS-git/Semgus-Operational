using System.Collections.Generic;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public record SynthesisTask (
        IReadOnlyList<string> Files,
        IReadOnlyList<SynthesisSolverConfig> Solvers,
        bool HaltOnParseError
    ) {
        public static SynthesisTask FromToml(TomlTable table) => new(
            Files: table.GetAtomList<string>("files", required: true),
            Solvers: table.GetStructuredList("solver", SynthesisSolverConfig.FromToml, required: true),
            HaltOnParseError: table.GetValueOrDefault("halt_on_parse_error", defaultValue: true)
        );

        public static SynthesisTask WithDefaults(IReadOnlyList<string> files) => new(Files: files, Solvers: new[] { SynthesisSolverConfig.Default }, HaltOnParseError: true);
    }
}