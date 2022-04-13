using Semgus.Operational;
using Semgus.Model.Smt;
using Semgus.Model;

namespace Semgus {
    public static class SolutionBlockConverter {

        private static IReadOnlyList<SmtAttributeValue> AssertList(SmtAttributeValue val) => val.Type == SmtAttributeValue.AttributeType.List ? val.ListValue! : throw new ArgumentException("Expected list");
        private static SmtIdentifier AssertId(SmtAttributeValue val) => val.Type == SmtAttributeValue.AttributeType.Identifier ? val.IdentifierValue! : throw new ArgumentException("Expected identifier");

        public static IEnumerable<SolutionBlock> ProcessAttributeValue(IReadOnlyCollection<SemgusSynthFun> synthFuns, InterpretationLibrary lib, SmtAttributeValue group) => AssertList(group).Select(block => ProcessBlock(synthFuns, lib, AssertList(block)));

        public static SolutionBlock ProcessBlock(IReadOnlyCollection<SemgusSynthFun> synthFuns, InterpretationLibrary lib, IReadOnlyList<SmtAttributeValue> items) {
            var id = AssertId(items[0]);
            var sf = synthFuns.Where(sf => sf.Relation.Name == id).Single();
            return new(sf,items.Skip(1).Select(v => lib.ParseAST(v, (SemgusTermType)sf.Rank.ReturnSort)).ToList());
        }
    }
}
