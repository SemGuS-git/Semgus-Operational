using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public record Configuration(
        int StackLimit,
        IReadOnlyList<SynthesisConfig> Solvers
    ) {
        public static Configuration FromTomlTable(TomlTable table) => new(
            StackLimit: table.GetValueOrDefault<int>("stack_limit", int.MaxValue),
            Solvers: table.GetStructuredList("solver", SynthesisConfig.FromToml, required: false)
        );
        public static Configuration FromFile(string filePath) => FromTomlTable(Toml.Parse(File.ReadAllText(filePath)).ToModel());

        private static Configuration _default;
        public static Configuration Default => _default ??= new(
            StackLimit: 1000,
            Solvers: new[] {
                SynthesisConfig.Default
            }
        );

        public string GetBatchLabel(Program.Mode mode, DateTime now) => $"semgus-{mode}.{now:yyyyMMdd-HHmm}";

        public LoggerConfiguration MakeLogCfg(LogEventLevel logLevel, string batchLabel) {
            var cfg = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console()
                .WriteTo.File(batchLabel + ".log");
            return cfg;
        }

    }
}