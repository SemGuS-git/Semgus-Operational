namespace Semgus.Operational {
    public class SmtStringsTheoryImpl : TemplateBasedTheoryImpl {
        public static SmtStringsTheoryImpl Instance { get; } = new();

        private SmtStringsTheoryImpl() : base(MakeTemplates()) { }

        private static FunctionTemplate[] MakeTemplates() => new FunctionTemplate[0]; // TODO

    }
}