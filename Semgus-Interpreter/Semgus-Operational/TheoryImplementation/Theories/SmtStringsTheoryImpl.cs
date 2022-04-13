using Semgus.TheoryImplementation;

namespace Semgus.Operational {
    public class SmtStringsTheoryImpl : TemplateBasedTheoryImpl {
        public SmtStringsTheoryImpl(ISortHelper sortHelper) : base(MakeTemplates()) { }

        private static FunctionTemplate[] MakeTemplates() => new FunctionTemplate[0]; // TODO

    }
}