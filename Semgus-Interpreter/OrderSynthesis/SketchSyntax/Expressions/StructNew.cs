using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record StructNew(Identifier TypeId, IReadOnlyList<Assignment> Args) : IExpression {

        private IReadOnlyDictionary<Identifier, int>? _dict = null;
        private IReadOnlyDictionary<Identifier, int> Dict => _dict ??=
            new Dictionary<Identifier, int>(Args.Select((a, i) => new KeyValuePair<Identifier, int>(((VariableRef)a.Subject).TargetId, i)));

        public bool TryGetPropValue(Identifier id, out IExpression value) {
            if (Dict.TryGetValue(id, out var index)) {
                value = Args[index].Value;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        public StructNew(Identifier typeId, params Assignment[] args) : this(typeId, args.ToList()) { }

        public override string ToString() => $"new {TypeId}({string.Join(", ", Args)})";

        public virtual bool Equals(StructNew? other) => other is not null && TypeId.Equals(other.TypeId) && Args.SequenceEqual(other.Args);
        public StructNew WithOverwrites(IReadOnlyDictionary<Identifier,IExpression> overwrites) {
            var a = Args.ToArray();
            foreach (var kvp in overwrites) {
                if (!Dict.TryGetValue(kvp.Key, out var i)) throw new KeyNotFoundException();
                a[i] = new(Args[i].Subject, kvp.Value);
            }
            return this with { Args = a, _dict = this._dict };
        }
    }
}
