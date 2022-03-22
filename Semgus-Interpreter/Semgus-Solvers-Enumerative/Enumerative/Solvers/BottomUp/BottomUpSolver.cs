using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Semgus.Solvers.Enumerative {
    public class BottomUpSolver : IEnumerativeSolver {
        public class Result : ISynthesisResult {
            public SolverInputInfo InputInfo { get; set; }
            public string MethodName { get; } = "BottomUp";

            public ConfigParameters Config { get; }

            public SynthesisStopCode StopCode { get; }
            public IDSLSyntaxNode Program { get; }
            public TimeSpan Runtime { get; }

            public BottomUpLoop.RunInfo RunInfo { get; }

            public Result(ConfigParameters config, BottomUpLoop.RunInfo runInfo, TimeSpan runtime) {
                this.Config = config;
                this.RunInfo = runInfo;
                this.StopCode = Transform(runInfo.Outcome);
                this.Program = runInfo.Program;
                this.Runtime = runtime;
            }

            private SynthesisStopCode Transform(BottomUpLoop.StopReason outcome) => outcome switch {
                BottomUpLoop.StopReason.Success => SynthesisStopCode.SAT,
                BottomUpLoop.StopReason.ExhaustedSearch => SynthesisStopCode.UNSAT,
                BottomUpLoop.StopReason.HitStopCondition => SynthesisStopCode.Timeout,
                BottomUpLoop.StopReason.HitCostLimit => SynthesisStopCode.Bound,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public ILogger Logger { get => _logger; set { _logger = value; _interpreter.Logger = value; } }
        private ILogger _logger;

        public ConfigParameters Config { get; }

        private readonly InterpreterHost _interpreter;

        public BottomUpSolver(ConfigParameters config) {
            Config = config;
            _interpreter = new InterpreterHost(config.InterpreterMaxDepth);
        }

        //public ISynthesisResult Run(SemgusProblem problem, Theory theory) {
        //    var grammar = InterpretationGrammar.FromAst(problem, theory);
        //    var spec = new InductiveConstraintAnalyzer(theory).Analyze(problem);
        //    return Run(grammar, spec);
        //}

        public ISynthesisResult Run(InterpretationGrammar grammar, InductiveConstraint spec) {
            Logger?.LogInformation("Starting BottomUpSolver with cost function {0}", Config.CostFunction);

            var bank = new ExpressionBank();

            using var disposable = new CompositeDisposable();
            var reductions = new List<IReduction>();

            if (Config.Reductions.Contains(ReductionMethod.Rewrite)) {
                var egg = new EggReduction(Config.RewriteRules) { Logger = Logger };
                reductions.Add(egg);
                disposable.Add(egg);
            }

            ITermReceiver receiver = Config.Reductions.Contains(ReductionMethod.ObservationalEquivalence)
                ? new InductiveObsEquivReceiver(_interpreter, spec, reductions)
                : new InductiveBasicReceiver(_interpreter, spec, reductions);

            Logger?.LogDebug("Using receiver {0} with reductions {1}", receiver.GetType().Name, string.Join(", ", reductions.Select(r => r.GetType().Name)));

            var enumerator = MakeTermEnumerator(grammar, bank);

            StopCondition stop;
            if (Config.Timeout.HasValue) {
                stop = new StopCondition(Config.Timeout.Value);
                stop.Start();
            } else {
                stop = null;
            }

            var timer = new Stopwatch();
            timer.Start();

            var maxCost = Config.MaxCost.GetValueOrDefault(int.MaxValue);

            var loop = new BottomUpLoop { Logger = Logger };
            var runInfo = loop.Run(receiver, bank, enumerator, stop, 1, maxCost);

            timer.Stop();

            if (runInfo.Outcome == BottomUpLoop.StopReason.Success) {
                Logger?.LogInformation("BottomUpSolver success ({0} t, {1}s)", runInfo.TermsEnumerated, timer.Elapsed.TotalSeconds);
                Logger?.LogDebug("Result program: {0}", runInfo.Program);
            } else {
                Logger?.LogInformation("BottomUpSolver fail ({0} t, {1}s)", runInfo.TermsEnumerated, timer.Elapsed.TotalSeconds);
            }
            return new Result(Config, runInfo, timer.Elapsed);
        }



        private ITermEnumerator MakeTermEnumerator(InterpretationGrammar grammar, ExpressionBank bank) {
            switch (Config.CostFunction) {
                case TermCostFunction.Size: return new CostSumEnumerator(DSLSyntaxNode.Factory.Instance, WeightedGrammar.SizeBased(grammar), bank);
                case TermCostFunction.Height: return new HeightEnumerator(DSLSyntaxNode.Factory.Instance, grammar, bank);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}