using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public record TaskGroup(
         IReadOnlyList<string> Includes,
         IReadOnlyList<UnitTestTask> UnitTestTasks,
         IReadOnlyList<InterpreterTask> InterpreterTasks,
         IReadOnlyList<SynthesisTask> SynthesisTasks
    ) {
        public static TaskGroup FromTomlTable(TomlTable table) => new(
            Includes: table.GetAtomList<string>("includes", required: false),
            UnitTestTasks: table.GetStructuredList("unit_test", UnitTestTask.FromToml, required: false),
            InterpreterTasks: table.GetStructuredList("interpreter_task", InterpreterTask.FromToml, required: false),
            SynthesisTasks: table.GetStructuredList("synth_task", SynthesisTask.FromToml, required: false)
        );

        public static Located<TaskGroup> FromFile(string filePath, string stem) => new(
            WorkingDirectory: Directory.GetParent(filePath).FullName,
            Stem: stem,
            Value: FromTomlTable(Toml.Parse(File.ReadAllText(filePath)).ToModel())
        );
    }
}