// See https://aka.ms/new-console-template for more information
using Semgus;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;
using Semgus.Parser;
using Semgus.Constraints;
using Semgus.Solvers.Enumerative;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.Logging;

public static class Program {
    public static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        var fn = args[0];
        Console.WriteLine(fn);


        var parser = new SemgusParser(fn);

        var handler = new MyHandler();
        parser.TryParse(handler);

        Console.WriteLine("OK 1");


        var lib = OperationalConverter.ProcessProductions(handler.OutSmt!.Theories, handler.OutSem!.Chcs.ToList());

        Console.WriteLine("OK 2");

        var grammar = OperationalConverter.ProcessGrammar(handler.OutSem.SynthFuns.First().Grammar, lib);

        Console.WriteLine("OK 3");

        var sf = handler.OutSem.SynthFuns.Single();
        var bc = handler.OutSem.Constraints.Where(c=>InductiveConstraintConverter.IsShapedLikeBehaviorExampleFor(sf,c)).ToList();

        var spec = InductiveConstraintConverter.ProcessConstraints(sf, bc, lib.Relations);

        Console.WriteLine("OK 4");
        var solver = new BottomUpSolver(new() { CostFunction = TermCostFunction.Size, Reductions = new(){ ReductionMethod.ObservationalEquivalence} });

        using var innerLogger = MakeLogCfg(LogEventLevel.Verbose).CreateLogger();
        var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(Program));
        solver.Logger = logger;

        var result = solver.Run(grammar, spec);



        Console.WriteLine("OK 5");
    }

    private static LoggerConfiguration MakeLogCfg(LogEventLevel logLevel) =>
        new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console();

    public class MyHandler : ISemgusProblemHandler {
        public SmtContext? OutSmt { get; private set; }
        public SemgusContext? OutSem { get; private set; }

        public void OnCheckSynth(SmtContext smtCtx, SemgusContext semgusCtx) {
            this.OutSmt = smtCtx;
            this.OutSem = semgusCtx;
        }

        public void OnConstraint(SmtContext smtCtx, SemgusContext semgusCxt, SmtTerm constraint) {
            
        }

        public void OnSetInfo(SmtContext ctx, SmtAttribute attr) {

        }

        public void OnSynthFun(SmtContext ctx, SmtIdentifier name, IList<SmtConstant> args, SmtSort sort) {

        }

        public void OnTermTypes(IReadOnlyList<SemgusTermType> termTypes) {

        }
    }
}
