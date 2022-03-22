namespace Semgus {
    public sealed record RelationInfo(string Name, IReadOnlyList<RelationSlotInfo> Slots) {
        public bool Equals(RelationInfo? other) => other is not null && Name == other.Name && Slots.SequenceEqual(other.Slots);

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 23 + Name.GetHashCode().GetHashCode();
                foreach (var u in Slots) {
                    hash = hash * 23 + u.GetHashCode();
                }
                return hash;
            }
        }
    }
}
