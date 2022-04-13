using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Semgus.Solvers.Enumerative {
    public class TopDownSolver : IEnumerativeSolver {

        public class Result : ISynthesisResult {
            public SolverInputInfo InputInfo { get; set; }
            public string MethodName { get; } = "TopDown";

            public ConfigParameters Config { get; }

            public SynthesisStopCode StopCode { get; }
            public IDSLSyntaxNode Program { get; }
            public TimeSpan Runtime { get; }

            public TopDownLoop.RunInfo RunInfo { get; }

            public Result(ConfigParameters config, TopDownLoop.RunInfo runInfo, TimeSpan runtime) {
                this.Config = config;
                this.RunInfo = runInfo;
                this.StopCode = Transform(runInfo.Outcome);
                this.Program = runInfo.Program;
                this.Runtime = runtime;
            }

            private SynthesisStopCode Transform(TopDownLoop.StopReason outcome) => outcome switch {
                TopDownLoop.StopReason.Success => SynthesisStopCode.SAT,
                TopDownLoop.StopReason.ExhaustedSearch => SynthesisStopCode.UNSAT,
                TopDownLoop.StopReason.HitStopCondition => SynthesisStopCode.Timeout,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public ILogger Logger { get => _logger; set { _logger = value; _interpreter.Logger = value; } }
        private ILogger _logger;

        public ConfigParameters Config { get; }

        private readonly InterpreterHost _interpreter;

        public TopDownSolver(ConfigParameters config) {
            Config = config;
            _interpreter = new InterpreterHost(config.InterpreterMaxDepth);
        }

        //public ISynthesisResult Run(SemgusProblem problem, Theory theory) {
        //    var grammar = InterpretationGrammar.FromAst(problem, theory);
        //    var spec = new InductiveConstraintAnalyzer(theory).Analyze(problem);
        //    return Run(grammar, spec);
        //}


        public ISynthesisResult Run(InterpretationGrammar grammar, InductiveConstraint spec) {
            if (Config.CostFunction != TermCostFunction.Size) throw new NotSupportedException();
            Logger?.LogInformation("Starting TopDownSolver with cost function {0}", Config.CostFunction);


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

            StopCondition stop;
            if (Config.Timeout.HasValue) {
                stop = new StopCondition(Config.Timeout.Value);
                stop.Start();
            } else {
                stop = null;
            }

            var timer = new Stopwatch();
            timer.Start();


            var loop = new TopDownLoop { Logger = Logger };
            var runInfo = loop.Run(receiver, grammar, spec.StartSymbol, stop);

            timer.Stop();

            if (runInfo.Outcome == TopDownLoop.StopReason.Success) {
                Logger?.LogInformation("TopDownSolver success ({0} t, {1}s)", runInfo.TermsEnumerated, timer.Elapsed.TotalSeconds);
                Logger?.LogInformation("Result program: {0}", runInfo.Program);
            } else {
                Logger?.LogInformation("TopDownSolver fail ({0} t, {1}s)", runInfo.TermsEnumerated, timer.Elapsed.TotalSeconds);
            }
            return new Result(Config, runInfo, timer.Elapsed);
        }
    }
}