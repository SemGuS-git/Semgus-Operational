using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Semgus.Operational;
using Semgus.Util;

namespace Semgus.Solvers {
    public class WeightedRuleGroup : IReadOnlyDictionary<int, IReadOnlyList<NonterminalProduction>> {
        private readonly DictOfList<int, NonterminalProduction> _dict;

        public WeightedRuleGroup(DictOfList<int, NonterminalProduction> dict) {
            _dict = dict;
        }

        public IReadOnlyList<NonterminalProduction> this[int key] => _dict[key];
        public IEnumerable<int> Keys => _dict.Keys;
        public IEnumerable<IReadOnlyList<NonterminalProduction>> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool ContainsKey(int key) => _dict.ContainsKey(key);

        public IEnumerator<KeyValuePair<int, IReadOnlyList<NonterminalProduction>>> GetEnumerator() => _dict.Select(
            kvp => new KeyValuePair<int, IReadOnlyList<NonterminalProduction>>(kvp.Key, kvp.Value)
        ).GetEnumerator();

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out IReadOnlyList<NonterminalProduction> value) {
            var b = _dict.TryGetValue(key, out var list);
            value = b ? list : default;
            return b;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dict).GetEnumerator();
    }
}