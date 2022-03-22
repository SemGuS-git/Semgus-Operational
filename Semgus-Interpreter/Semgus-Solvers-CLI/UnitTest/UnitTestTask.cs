using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {

    public class UnitTestTask {
        public IReadOnlyList<string> Files { get; init; }
        public IReadOnlyList<UnitTest> Tests { get; init; }

        public static UnitTestTask FromToml(TomlTable table) => new() {
            Files = table.GetAtomList<string>("files", required: true),
            Tests = table.GetStructuredList("test", UnitTest.FromToml, required: true),
        };
    }
}