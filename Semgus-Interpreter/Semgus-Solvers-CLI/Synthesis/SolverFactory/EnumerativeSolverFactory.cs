using Semgus.Solvers.Enumerative;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public class EnumerativeSolverFactory {
        public static EnumerativeSolverFactory Instance { get; } = new();
        private EnumerativeSolverFactory() { }

        public IEnumerativeSolver Instantiate(SynthesisConfig cfg) => cfg.MethodName switch {
            "bottom_up" => new BottomUpSolver(ToTopDownConfig(cfg.Timeout, cfg.Params)),
            "top_down" => new TopDownSolver(ToTopDownConfig(cfg.Timeout, cfg.Params)),
            _ => throw new KeyNotFoundException(),
        };

        private ConfigParameters ToTopDownConfig(TimeSpan? timeout, IReadOnlyDictionary<string, object> parameters) {
            object v;

            var cfg = new ConfigParameters() {
                CostFunction = parameters.TryGetValue("cost_function", out v)
                    ? GetCostFunction((string)v)
                    : throw new ArgumentException("Missing required parameter `cost_function`"),

                Reductions = parameters.TryGetValue("reductions", out v)
                    ? ((IEnumerable)v).Cast<string>().Select(GetReductionMethod).ToList()
                    : new(),

                Timeout = timeout,
            };

            if (parameters.TryGetValue("interpreter_max_depth", out v)) cfg.InterpreterMaxDepth = Convert.ToInt32(v);
            if (parameters.TryGetValue("max_cost", out v)) cfg.MaxCost = Convert.ToInt32(v);
            if (parameters.TryGetValue("rewrite_rules", out v)) cfg.RewriteRules = ((IEnumerable)v).Cast<string>().ToList();

            return cfg;
        }

        private static ReductionMethod GetReductionMethod(string s) => s switch {
            "rewrite" => ReductionMethod.Rewrite,
            "observational_equivalence" => ReductionMethod.ObservationalEquivalence,
            _ => throw new ArgumentException($"Unknown reduction method {s}")
        };

        private static TermCostFunction GetCostFunction(string v) => v switch {
            "size" => TermCostFunction.Size,
            "height" => TermCostFunction.Height,
            _ => throw new ArgumentException(v),
        };
    }
}