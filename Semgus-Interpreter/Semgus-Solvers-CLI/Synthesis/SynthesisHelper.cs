using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Operational;
using Semgus.Solvers;
using System;
using System.Collections.Generic;

namespace Semgus.CommandLineInterface {
    public class SynthesisHelper {
        private record WorkItem(string ProblemIdentifier, InterpretationGrammar Grammar, InductiveConstraint Spec, SynthesisSolverConfig SolverConfig);

        public ILogger Logger { get; set; }

        // note: currently all tasks are digested prior to starting synthesis on any of them.
        public IEnumerable<ISynthesisResult> RunAll(IEnumerable<Located<SynthesisTask>> tasks, string batchName) {
            List<FileParseErrorResult> parseErrors = new();
            List<WorkItem> workList = GetWorkItems(tasks, batchName, parseErrors);

            foreach (var errorResult in parseErrors) yield return errorResult;

            var solverFactory = EnumerativeSolverFactory.Instance;  

            for (int i = 0; i < workList.Count; i++) {
                var workItem = workList[i];
                Logger?.LogInformation("[{0}/{1}] Beginning work on {name}", i + 1, workList.Count, workItem.ProblemIdentifier);

                var solver = solverFactory.Instantiate(workItem.SolverConfig);
                solver.Logger = Logger;

                var startTime = DateTime.Now;
                var result = solver.Run(workItem.Grammar, workItem.Spec); // run solver
                result.InputInfo = new SolverInputInfo(startTime, workItem.ProblemIdentifier, batchName);
                Logger?.LogInformation("[{0}/{1}] Done", i + 1, workList.Count);
                yield return result;
            }

            Logger?.LogInformation("All work items complete");
        }

        private List<WorkItem> GetWorkItems(IEnumerable<Located<SynthesisTask>> all, string batchName, List<FileParseErrorResult> errors) {
            List<WorkItem> workList = new();

            foreach (var locTask in all) {
                var task = locTask.Value;

                foreach (var problemFileName in task.Files) {
                    try {
                        // Parse all files first to avoid surprise errors later
                        var items = ParseUtil.TypicalItems.Acquire(locTask.GetFilePath(problemFileName));

                        foreach (var solverConfig in task.Solvers) {
                            workList.Add(new WorkItem(locTask.GetIdentifier(problemFileName), items.Grammar, items.Constraint, solverConfig));
                        }
                    } catch (Exception e) {
                        Logger?.LogError("Error occurred while processing {problemFile}:\n{err}", problemFileName, e);
                        if (task.HaltOnParseError) {
                            Logger?.LogInformation("Halting");
                            throw new SemgusFileProcessingException(problemFileName, e);
                        } else {
                            Logger?.LogInformation("Ignoring problem file {problemFile}", problemFileName);
                            errors.Add(new FileParseErrorResult(e) { InputInfo = new SolverInputInfo(DateTime.Now, problemFileName, batchName) });
                        }
                    }

                }
            }
            return workList;
        }
    }
        

}
