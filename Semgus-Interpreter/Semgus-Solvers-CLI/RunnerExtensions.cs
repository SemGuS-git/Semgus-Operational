using System.Collections.Generic;

namespace Semgus.CommandLineInterface {
    public static class RunnerExtensions {
        public static void RunAll(this IRunner runner, IReadOnlyList<string> inputFiles) {
            foreach (var file in inputFiles) runner.Run(file);
        }
    }
}