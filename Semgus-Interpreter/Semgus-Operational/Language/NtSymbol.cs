namespace Semgus.Operational {
    /// <summary>
    /// Simplified nonterminal identifier for use in the operational codebase.
    /// </summary>
    /// <param name="Name"></param>
    public record NtSymbol(string Name) {
        public override string ToString() => Name;
        public override int GetHashCode() => Name.GetHashCode();
    }
}