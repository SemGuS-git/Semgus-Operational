using Microsoft.Extensions.Logging;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Operational;
using Semgus.Model.Smt.Terms;
using Semgus.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Semgus.Constraints;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public static class ParseUtil {
        public class TypicalItems {
            public SmtContext Smt { get; }
            public SemgusContext Sem { get; }
            public SemgusSynthFun SynthFun { get; }
            public InterpretationLibrary Library { get; }
            public InterpretationGrammar Grammar { get; }
            public InductiveConstraint Constraint { get; }
            public IReadOnlyList<DemoBlock> Tests { get; }
            public IReadOnlyList<SolutionBlock> Solutions { get; }

            public TypicalItems(SmtContext smt, SemgusContext sem, SemgusSynthFun synthFun, InterpretationLibrary library, InterpretationGrammar grammar, InductiveConstraint constraint, IReadOnlyList<DemoBlock> tests, IReadOnlyList<SolutionBlock> solutions) {
                Smt = smt;
                Sem = sem;
                SynthFun = synthFun;
                Library = library;
                Grammar = grammar;
                Constraint = constraint;
                Tests = tests;
                Solutions = solutions;
            }

            public static TypicalItems Acquire(string filePath, ILogger logger = null) {
                var (smt, sem, info) = ParseUtil.ParseFile(filePath, logger);
                var lib = OperationalConverter.ProcessProductions(smt, smt.Theories, sem.Chcs);
                var sf = sem.SynthFuns.Single();
                var grammar = OperationalConverter.ProcessGrammar(sf.Grammar, lib);
                var spec = new InductiveConstraintConverter(lib.Theory,sf,lib.SemanticRelations).ProcessConstraints(sem.Constraints);

                var tests = info.Where(a => a.Keyword.Name == "test").SelectMany(a => DemoBlockConverter.ProcessAttributeValue(lib, a.Value)).ToList();
                var solns = info.Where(a => a.Keyword.Name == "solution").SelectMany(a => SolutionBlockConverter.ProcessAttributeValue(sem.SynthFuns, lib, a.Value)).ToList();

                return new(smt, sem, sf, lib, grammar, spec, tests, solns);
            }
        }

        public static (SmtContext smt, SemgusContext sem, IReadOnlyList<SmtAttribute> info) ParseFile(string filePath,ILogger logger = null) {
            if(TryParseFile(filePath,logger,out var smt, out var sem,out var info)) {
                return (smt, sem, info);
            } else {
                throw new Exception("Parsing failed");
            }
        }

        public static bool TryParseFile(string filePath, ILogger logger, out SmtContext smt, out SemgusContext sem, out IReadOnlyList<SmtAttribute> info) {

            var parser = new SemgusParser(filePath);

            var handler = new OneShotHandler();

            bool ok;

            if (logger is null) {
                using var errWriter = new StringWriter();
                ok = parser.TryParse(handler, errWriter);
                var errText = errWriter.ToString();
                if (errText != null && errText.Length > 0) {
                    logger.LogWarning(errText);
                }
            } else {
                ok = parser.TryParse(handler);
            }

            if(ok && ! handler.HitCheckSynth) {
                throw new NotSupportedException("Missing check-synth");
            }

            if (ok) {
                smt = handler.OutSmt!;
                sem = handler.OutSem!;
                info = handler.Info;
            } else { 
                smt = default;
                sem = default;
                info = default;
            }

            return ok;
        }

        private class OneShotHandler : ISemgusProblemHandler {
            public bool HitCheckSynth { get; private set; } = false;

            public SmtContext? OutSmt { get; private set; }
            public SemgusContext? OutSem { get; private set; }
            public List<SmtAttribute> Info { get; } = new();

            public void OnCheckSynth(SmtContext smtCtx, SemgusContext semgusCtx) {
                if (HitCheckSynth) throw new NotSupportedException("Multiple check-synth");
                HitCheckSynth = true;
                this.OutSmt = smtCtx;
                this.OutSem = semgusCtx;
            }

            public void OnConstraint(SmtContext smtCtx, SemgusContext semgusCxt, SmtTerm constraint) {
                if (HitCheckSynth) throw new NotSupportedException("constraint after check-synth");
            }

            public void OnSetInfo(SmtContext ctx, SmtAttribute attr) {
                Info.Add(attr);
            }

            public void OnSynthFun(SmtContext ctx, SmtIdentifier name, IList<(SmtIdentifier, SmtSortIdentifier)> args, SmtSort sort)
            {
                if (HitCheckSynth) throw new NotSupportedException("synth-fun after check-synth");
            }

            public void OnTermTypes(IReadOnlyList<SemgusTermType> termTypes) {
                if (HitCheckSynth) throw new NotSupportedException("declare-term-types after check-synth");

            }
        }
    }
}
