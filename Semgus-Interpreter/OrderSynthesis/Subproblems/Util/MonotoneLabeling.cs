using Semgus.OrderSynthesis.SketchSyntax;
using System.Diagnostics;
using System.Text.Json;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class MonotoneLabeling {
        public bool Any => ArgMonotonicities.Any(m => m != Monotonicity.None);
        public FunctionDefinition Function { get; }
        public IReadOnlyList<Monotonicity> ArgMonotonicities { get; }

        public MonotoneLabeling(FunctionDefinition function, IReadOnlyList<Monotonicity> argMonotonicities) {
            if (function.Signature.Args.Count != argMonotonicities.Count) throw new ArgumentException();
            Function = function;
            ArgMonotonicities = argMonotonicities;
        }

        public static MonotoneLabeling ZeroArgument(FunctionDefinition function) => new(function, Array.Empty<Monotonicity>());

        public static async Task<IEnumerable<MonotoneLabeling>> ExtractFromJson(IReadOnlyList<FunctionDefinition> functions, string fname) {
            using var fs = File.OpenRead(fname);

            var obj = await JsonSerializer.DeserializeAsync<IReadOnlyDictionary<string, IReadOnlyList<string>>>(fs);

            Debug.Assert(obj.Count == functions.Count);

            return functions.Select(fn => new MonotoneLabeling(fn, obj[fn.Id.Name].Select(s => Enum.Parse<Monotonicity>(s, true)).ToList()));
        }
    }
}
