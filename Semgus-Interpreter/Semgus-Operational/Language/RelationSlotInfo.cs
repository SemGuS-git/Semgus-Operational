using Semgus.Model.Smt;

namespace Semgus {
    public sealed record RelationSlotInfo(SmtSort Sort, string TopLevelVarName, RelationSlotLabel Label);
}
