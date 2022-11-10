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
using Semgus.Operational;

public static class Program {
    public static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        var fn = args[0];
        Console.WriteLine(fn);


        var parser = new SemgusParser(fn);

        var handler = new MyHandler();
        parser.TryParse(handler);

        Console.WriteLine("OK 1");


        var lib = OperationalConverter.ProcessProductions(handler.OutSmt!, handler.OutSem!.Chcs.ToList());

        Console.WriteLine("OK 2");

        var grammar = OperationalConverter.ProcessGrammar(handler.OutSem.SynthFuns.First().Grammar, lib);

        Console.WriteLine("OK 3");

        var sf = handler.OutSem.SynthFuns.Single();
        var bc = handler.OutSem.Constraints.Where(c => InductiveConstraintConverter.IsShapedLikeBehaviorExampleFor(sf, c)).ToList();

        var spec = new InductiveConstraintConverter(lib.Theory, sf, lib.SemanticRelations).ProcessConstraints(bc);

        Console.WriteLine("OK 4");
        var solver = new BottomUpSolver(new() { CostFunction = TermCostFunction.Size, Reductions = new() { ReductionMethod.ObservationalEquivalence } });

        using var innerLogger = MakeLogCfg(LogEventLevel.Verbose).CreateLogger();
        var logger = new SerilogLoggerProvider(innerLogger).CreateLogger(nameof(Program));
        solver.Logger = logger;

        var tests = handler.Tests.SelectMany(a => DemoBlockConverter.ProcessAttributeValue(lib, a)).ToList();
        var solns = handler.Solutions.Select(a => lib.ParseAST(a)).ToList();

        var interpreter = new InterpreterHost(1000);
        var result0 = interpreter.RunProgram(tests[0].Program, tests[0].ArgLists[0]);

        Console.WriteLine("OK 5");

        var result = solver.Run(grammar, spec);



        Console.WriteLine("OK 6");
    }


    private static LoggerConfiguration MakeLogCfg(LogEventLevel logLevel) =>
        new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console();

    public class MyHandler : ISemgusProblemHandler {
        public SmtContext? OutSmt { get; private set; }
        public SemgusContext? OutSem { get; private set; }

        public List<SmtAttributeValue> Tests { get; } = new();
        public List<SmtAttributeValue> Solutions { get; } = new();


        public void OnCheckSynth(SmtContext smtCtx, SemgusContext semgusCtx) {
            this.OutSmt = smtCtx;
            this.OutSem = semgusCtx;
        }

        public void OnConstraint(SmtContext smtCtx, SemgusContext semgusCxt, SmtTerm constraint) {

        }

        public void OnSetInfo(SmtContext ctx, SmtAttribute attr) {
            switch (attr.Keyword.Name) {
                case "test":
                    Tests.Add(attr.Value);
                    break;
                case "solution":
                    Solutions.Add(attr.Value);
                    break;
            }
        }

        public void OnSynthFun(SmtContext ctx, SmtIdentifier name, IList<(SmtIdentifier, SmtSortIdentifier)> args, SmtSort sort) {
        }

        public void OnTermTypes(IReadOnlyList<SemgusTermType> termTypes) {

        }
    }
}
