using Microsoft.Extensions.Logging;
using Semgus.Operational;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Semgus.Solvers.Enumerative {
    public class BottomUpLoop {
        public enum StopReason {
            Success,
            HitCostLimit,
            HitStopCondition,
            ExhaustedSearch,
        }

        public class RunInfo {
            public class AtCostLevel {
                public bool RanToEnd { get; set; }
                public int TermsEnumerated { get; set; }
                public int TermsDiscarded { get; set; }
                public int FinalBankSize { get; set; }
                public TimeSpan Runtime { get; set; }
            }

            public StopReason Outcome { get; set; }
            public IDSLSyntaxNode Program { get; set; }
            
            public int CostStart { get; set; }
            public int CostReached { get; set; }
            public int TermsEnumerated { get; set; }
            public int FinalBankSize { get; set; }
            public TimeSpan Runtime { get; set; }
            public Dictionary<int, AtCostLevel> CostLevels { get; set; }
        }

        public ILogger Logger { get; set; }

        public RunInfo Run(ITermReceiver receiver, ExpressionBank bank, ITermEnumerator enumerator, StopCondition stop, int startBudget, int maxCost) {
            if (maxCost <= startBudget) throw new ArgumentException();

            int termsEnumerated = 0;
            int budget;

            bool isLogDebug = Logger?.IsEnabled(LogLevel.Debug) ?? false;
            const double LOG_TIME_STEP = 10.0;
            double nextLogTime = LOG_TIME_STEP;

            var outerTimer = new Stopwatch();
            var innerTimer = new Stopwatch();
            outerTimer.Start();

            var costLevels = new Dictionary<int, RunInfo.AtCostLevel>();

            RunInfo MakeOutput(StopReason outcome, IDSLSyntaxNode program = null) => new RunInfo {
                Outcome = outcome,
                Program = program,
                CostStart = startBudget,
                CostReached = budget,
                TermsEnumerated = termsEnumerated,
                FinalBankSize = bank.Size,
                Runtime = outerTimer.Elapsed,
                CostLevels = costLevels,
            };
            
            for (budget = startBudget; budget < maxCost; budget++) {

                Logger?.LogTrace("Starting enumeration at cost {budget}", budget);

                var distinctTerms = new List<IDSLSyntaxNode>();
                int termsEnumeratedAtCost = 0;
                int termsDiscardedAtCost = 0;

                void RecordCostLevelInfo(bool ranToEnd) => costLevels.Add(budget, new RunInfo.AtCostLevel {
                    RanToEnd = ranToEnd,
                    TermsEnumerated = termsEnumeratedAtCost,
                    TermsDiscarded = termsDiscardedAtCost,
                    FinalBankSize = bank.Size + distinctTerms.Count,
                    Runtime = innerTimer.Elapsed,
                });

                innerTimer.Restart();

                foreach (var expr in enumerator.EnumerateAtCost(budget)) {
                    Logger?.LogTrace("-> {0}", expr);
                    termsEnumerated++;
                    termsEnumeratedAtCost++;

                    switch(receiver.Receive(expr)) {
                        case TermReceiverCode.ReturnSolution:
                            Logger?.LogDebug("Found a match after {k} terms: {expr}", termsEnumerated, expr);
                            RecordCostLevelInfo(false);
                            return MakeOutput(StopReason.Success, expr);
                        case TermReceiverCode.Retain:
                            Logger?.LogTrace("Adding bank term {expr}", expr);
                            distinctTerms.Add(expr);
                            break;
                        case TermReceiverCode.Prune:
                            Logger?.LogTrace("Prune {expr}", expr);
                            termsDiscardedAtCost++;
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }

                    if (stop?.IsStop() ?? false) {
                        Logger?.LogDebug("Halting (duration exceeded)");
                        RecordCostLevelInfo(false);
                        return MakeOutput(StopReason.HitStopCondition);
                    }
                    
                    if(isLogDebug && outerTimer.Elapsed.TotalSeconds >= nextLogTime) {
                        Logger.LogDebug("[{0}] Enumerated {1}, bank size {2}, cost {3}", outerTimer.Elapsed, termsEnumerated, bank.Size, budget);
                        nextLogTime += LOG_TIME_STEP;
                    }
                }

                foreach (var expr in distinctTerms) {
                    bank.Add(expr.Nonterminal, budget, expr);
                }
                distinctTerms.Clear();

                if (isLogDebug && outerTimer.Elapsed.TotalSeconds >= nextLogTime) {
                    Logger.LogDebug("[{0}] Enumerated {1}, bank size {2}", outerTimer.Elapsed, termsEnumerated, bank.Size);
                    nextLogTime += LOG_TIME_STEP;
                }

                RecordCostLevelInfo(true);

                if(termsEnumeratedAtCost==0) {
                    if(budget >= enumerator.GetHighestAvailableCost()) {
                        Logger?.LogDebug("Halting (exhausted language at cost {0})", budget);
                        return MakeOutput(StopReason.ExhaustedSearch);
                    }
                }
            }

            Logger?.LogDebug("Halting (reached cost limit {0})", maxCost);
            return MakeOutput(StopReason.HitCostLimit);
        }
    }
}