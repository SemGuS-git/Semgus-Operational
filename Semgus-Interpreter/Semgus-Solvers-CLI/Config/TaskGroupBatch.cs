using Semgus.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public record TaskGroupBatch(IReadOnlyList<Located<TaskGroup>> TaskGroups, string BatchName, string OutputPrefix, string RootFolder) {
        public static TaskGroupBatch ReadFileTree(string taskFilePath) {
            var root = TaskGroup.FromFile(taskFilePath, taskFilePath);
            return new(
                TaskGroups: IterationUtil.UnrollTree(root, TaskGroupExtensions.GetIncludes).ToList(),
                BatchName: Path.GetFileName(taskFilePath),
                OutputPrefix: GetOutputPrefix(taskFilePath, GetDTString()),
                RootFolder: root.WorkingDirectory
            );
        }

        private static string GetDTString() => DateTime.Now.ToString("yyMMdd-HHmm");

        private static string GetOutputPrefix(string taskFilePath, string sessionDt) =>
            Path.Combine(Directory.GetParent(taskFilePath).FullName, ".semgus-output", Path.GetFileName(taskFilePath) + "." + sessionDt);

        public static TaskGroupBatch DefaultSynthTask(string folder, IReadOnlyList<string> inputFiles) => new(
            TaskGroups: new[] {
                new Located<TaskGroup>(
                    folder,
                    folder,
                    new TaskGroup(
                        Includes: Array.Empty<string>(),
                        UnitTestTasks: Array.Empty<UnitTestTask>(),
                        InterpreterTasks: Array.Empty<InterpreterTask>(),
                        SynthesisTasks: new[] {
                            SynthesisTask.WithDefaults(inputFiles)
                        }

                    )
                )
            },
            BatchName: "session",
            OutputPrefix: Path.Combine(".", ".semgus-output", "session." + GetDTString()),
            RootFolder: folder
        );
    }
}