using System.Collections.Generic;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public static class TaskGroupExtensions {
        public static IEnumerable<Located<TaskGroup>> GetIncludes(this Located<TaskGroup> obj) =>
            obj.Value.Includes is null ?
            Enumerable.Empty<Located<TaskGroup>>() :
            obj.Value.Includes.Select(obj.GetFilePath).Select(f=>TaskGroup.FromFile(f,obj.Stem));


        public static IEnumerable<string> EnumerateFilePaths(this Located<TaskGroup> obj) =>
            obj.Value.UnitTestTasks.SelectMany(t => t.Files.Select(obj.GetFilePath))
            .Concat(obj.Value.InterpreterTasks.SelectMany(t => t.Files.Select(obj.GetFilePath)))
            .Concat(obj.Value.SynthesisTasks.SelectMany(t => t.Files.Select(obj.GetFilePath)));
    }
}