using Microsoft.Extensions.Logging;
using Semgus.Constraints;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public class ParseCheckSession {
        private enum ErrorPhase { Parse, Semantics, Shape, Grammar, Constraints };
        private record Error(string ProblemFileName, ErrorPhase Phase, Exception Exception);

        public ILogger Logger { get; set; }

        public void Run(TaskGroupBatch batch) {
            List<Error> errors = new();
            int passCount = 0, parseFailCount = 0, semFailCount = 0, consFailCount = 0;
            var distinctFiles = batch.TaskGroups.SelectMany(TaskGroupExtensions.EnumerateFilePaths).Distinct().ToList();

            int n = distinctFiles.Count;

            for (int i = 0; i < n; i++) {
                var filePath = distinctFiles[i];
                var fileName = Path.GetFileName(filePath);

                SmtContext smt;
                SemgusContext sem;

                SemgusSynthFun sf;

                InterpretationLibrary lib;
                InterpretationGrammar grammar;
                InductiveConstraint spec;

                try {
                    (smt, sem) = ParseUtil.ParseFile(filePath, Logger);
                } catch(Exception e) { 
                    Logger?.LogError("[{0}/{1}] FAIL on parse {2}", i + 1, n, fileName);
                    errors.Add(new(fileName, ErrorPhase.Parse, e));
                    parseFailCount++;
                    continue;
                }
                try {
                    lib = OperationalConverter.ProcessProductions(smt.Theories, sem.Chcs);
                } catch (Exception e) {
                    Logger?.LogError("[{0}/{1}] FAIL on semantics {2}", i + 1, n, fileName);
                    errors.Add(new(fileName, ErrorPhase.Semantics, e));
                    semFailCount++;
                    continue;
                }

                try {
                    sf = sem.SynthFuns.Single();
                } catch (Exception e) {
                    Logger?.LogError("[{0}/{1}] FAIL on assert single synth-fun {2}", i + 1, n, fileName);
                    errors.Add(new(fileName, ErrorPhase.Shape, e));
                    semFailCount++;
                    continue;
                }

                try {
                    grammar = OperationalConverter.ProcessGrammar(sf.Grammar, lib);
                } catch (Exception e) {
                    Logger?.LogError("[{0}/{1}] FAIL on grammar {2}", i + 1, n, fileName);
                    errors.Add(new(fileName, ErrorPhase.Grammar, e));
                    semFailCount++;
                    continue;
                }

                try {
                    spec = new InductiveConstraintConverter(lib.Theory,sf,lib.Relations).ProcessConstraints(sem.Constraints);
                } catch (Exception e) {
                    Logger?.LogError("[{0}/{1}] FAIL on constraints {2}", i + 1, n, fileName);
                    errors.Add(new(fileName, ErrorPhase.Constraints, e));
                    consFailCount++;
                    continue;
                }
                Logger?.LogInformation("[{0}/{1}] PASS {2}", i + 1, n, fileName);
                passCount++;
            }


            Logger?.LogInformation("PASS {0} of {1}, FAIL {2} of {3} ({4} parse, {5} sem, {6} cons, {7} other)",
                passCount, n, n - passCount, n,
                parseFailCount, semFailCount, consFailCount, n - (passCount + parseFailCount + semFailCount + consFailCount)
            );

            foreach (var err in errors) {
                Logger?.LogError("{0} hit {1} during {2}:\n{3}", err.ProblemFileName, err.Exception.GetType().Name, err.Phase, err.Exception);
            }

            Logger?.LogInformation("PASS {0} of {1}, FAIL {2} of {3} ({4} parse, {5} sem, {6} cons, {7} other)",
                passCount, n, n - passCount, n,
                parseFailCount, semFailCount, consFailCount, n - (passCount + parseFailCount + semFailCount + consFailCount)
            );

        }

    }
}