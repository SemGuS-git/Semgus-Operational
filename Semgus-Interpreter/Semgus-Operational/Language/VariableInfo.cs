using Semgus.Model.Smt;

namespace Semgus.Operational {
    /// <summary>
    /// Indicates the name and type of a variable that is referenced in a production rule's semantics.
    /// (not including child terms)
    /// </summary>
    /// 
    public sealed record VariableInfo (string Name, int Index, SmtSort Sort, VariableUsage Usage);
}