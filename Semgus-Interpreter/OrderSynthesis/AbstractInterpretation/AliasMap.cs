namespace Semgus.OrderSynthesis.AbstractInterpretation {

    // todo move to Util
    internal class AliasMap<TKey> {
        private Dictionary<TKey, TKey> _map = new();

        public bool IsAlias(TKey key) => _map.ContainsKey(key);

        public TKey Resolve(TKey alias) => TryGet(alias, out var value) ? value : alias;

        public bool TryGet(TKey alias, out TKey value) {
            bool ok = false;
            while(_map.TryGetValue(alias,out var next)) {
                ok = true;
                alias = next;
            }
            value = ok ? alias : default;
            return ok;
        }

        public void Register(TKey alias, TKey value) {
            if (alias.Equals(value)) return;

            while(_map.TryGetValue(value,out var next)) {
                if (alias.Equals(next)) return; // ok
                value = next;
            }

            // if there's already a target for this alias, make sure it's the same thing
            if(TryGet(alias,out var other)) {
                if (!value.Equals(other)) throw new ArgumentException("Key already mapped to something else");
                return;
            }

            _map.Add(alias, value);
        }
    }



    //internal class Agglom {
    //    SemgusTermType TermType { get; }
    //    SynthComparisonFunction CompareIn { get; }
    //    SynthComparisonFunction CompareOut { get; }
    //}

    //internal class Interval {

    //}
    //internal class SemRelArgTupleType {

    //}

    //internal class SynthComparisonFunction {
    //    public bool Leq() {

    //    }
    //}
}
