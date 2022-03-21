// See https://aka.ms/new-console-template for more information
using Semgus;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;
using Semgus.Parser;

public static class Program {
    public static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        var fn = args[0];
        Console.WriteLine(fn);


        var parser = new SemgusParser(fn);

        var handler = new MyHandler();
        parser.TryParse(handler);

        Console.WriteLine("OK 1");


        var lib = OperationalConverter.ProcessProductions(handler.OutSmt.Theories, handler.OutSem.Chcs.ToList());

        Console.WriteLine("OK 2");
    }

    public class MyHandler : ISemgusProblemHandler {
        public SmtContext OutSmt { get; private set; }
        public SemgusContext OutSem { get; private set; }

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
