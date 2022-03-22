using Semgus.Operational;
using Semgus.Model.Smt;

namespace Semgus {
    public static class DemoBlockConverter {

        private static IReadOnlyList<SmtAttributeValue> AssertList(SmtAttributeValue val) => val.Type == SmtAttributeValue.AttributeType.List ? val.ListValue! : throw new ArgumentException("Expected list");

        public static IEnumerable<DemoBlock> ProcessAttributeValue(InterpretationLibrary lib, SmtAttributeValue group) => AssertList(group).Select(block => ProcessBlock(lib, AssertList(block)));

        public static DemoBlock ProcessBlock(InterpretationLibrary lib, IReadOnlyList<SmtAttributeValue> items) => new(
            lib.ParseAST(items[0]),
            items.Skip(1).Select(item => ReadArgList(lib.Theory, item)).ToList()
        );

        public static object?[] ReadArgList(ITheoryImplementation theory, SmtAttributeValue val) {
            if (val.Type != SmtAttributeValue.AttributeType.List) throw new Exception();
            var items = val.ListValue!;

            if (items.Count == 0 || items[0].Type != SmtAttributeValue.AttributeType.Keyword || items[0].KeywordValue!.Name != "t") {
                throw new ArgumentException();
            }

            var n = items.Count - 1;

            var args = new object?[n];
            for (int i = 0; i < n; i++) {
                args[i] = theory.EvalConstant(items[i + 1]);
            }

            return args;
        }
    }
}
