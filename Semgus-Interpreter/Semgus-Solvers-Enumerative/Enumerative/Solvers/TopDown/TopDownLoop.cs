using Microsoft.Extensions.Logging;
using Semgus.Operational;
using Semgus.Solvers.Common;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Semgus.Solvers.Enumerative {
    public class TopDownLoop {
        public enum StopReason {
            Success,
            HitStopCondition,
            ExhaustedSearch,
        }

        public class RunInfo {
            public StopReason Outcome { get; set; }
            public IDSLSyntaxNode Program { get; set; }
            public int TermsEnumerated { get; set; }
            public TimeSpan Runtime { get; set; }
        }

        public ILogger Logger { get; set; }

        public RunInfo Run(ITermReceiver receiver, InterpretationGrammar grammar, NtSymbol startSymbol, StopCondition stop) {
            CostOrganizedQueues<IDSLSyntaxNode> workQueues = new();
            var ntCosts = GrammarCostGraph.ComputeMinAstSizes(grammar);
            
            var workQueue = new Queue<PartialProgramNode>();

            int termsEnumerated = 0;

            bool isLogDebug = Logger?.IsEnabled(LogLevel.Debug) ?? false;
            const double LOG_TIME_STEP = 10.0;
            double nextLogTime = LOG_TIME_STEP;

            var outerTimer = new Stopwatch();
            outerTimer.Start();

            RunInfo MakeOutput(StopReason outcome, IDSLSyntaxNode program = null) => new RunInfo {
                Outcome = outcome,
                Program = program,
                TermsEnumerated = termsEnumerated,
                Runtime = outerTimer.Elapsed,
            };

            if(grammar.Productions.TryGetValue(startSymbol, out var startProd)) {
                foreach(var prod in startProd) {
                    IDSLSyntaxNode expr = prod.IsLeaf() ? new DSLSyntaxNode(startSymbol, prod.Production) : PartialProgramNode.FromRule(prod, null, 0, ntCosts);
                    workQueues.EnqueueAt(expr.Size, expr);
                    Logger?.LogTrace("[${c} #{n}] {a} -> {b}", expr.Size, termsEnumerated, startSymbol, expr);
                    termsEnumerated++;
                }
            }

            int budget = 0;

            while (budget < workQueues.MaxCost) {
                var queue = workQueues.Get(budget);
                while (queue.TryDequeue(out var expr)) {
                    if (stop?.IsStop() ?? false) {
                        Logger?.LogDebug("Halting (duration exceeded)");
                        return MakeOutput(StopReason.HitStopCondition);
                    }
                    if (isLogDebug && outerTimer.Elapsed.TotalSeconds >= nextLogTime) {
                        Logger.LogDebug("[{0}] Enumerated {1}, cost {2}", outerTimer.Elapsed, termsEnumerated, budget);
                        nextLogTime += LOG_TIME_STEP;
                    }

                    // Apply reduction(s) within receiver
                    switch (receiver.Receive(expr)) {
                        case TermReceiverCode.ReturnSolution:
                            Logger?.LogDebug("Found a match after {k} terms: {expr}", termsEnumerated, expr);
                            return MakeOutput(StopReason.Success, expr);
                        case TermReceiverCode.Retain:
                            if (expr is PartialProgramNode partial) {
                                Logger?.LogTrace("Expanding {expr}", expr);
                                var hole = partial.GetFirstHole();
                                if (grammar.Productions.TryGetValue(hole.Nonterminal, out var holeProd)) {
                                    foreach (var prod in holeProd) {
                                        var next = hole.ReplaceWith(prod);
                                        workQueues.EnqueueAt(next.Size, next);
                                        Logger?.LogTrace("[${c} #{n}] {a} -> {b}", next.Size, termsEnumerated, partial, next);
                                        termsEnumerated++;
                                    }
                                }
                            }
                            break;
                        case TermReceiverCode.Prune:
                            Logger?.LogTrace("Prune {expr}", expr);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                // queue exhausted
                budget++;
            }
            Logger?.LogDebug("Halting (exhausted language)");
            return MakeOutput(StopReason.ExhaustedSearch);
        }
    }
}