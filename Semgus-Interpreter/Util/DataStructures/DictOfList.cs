using System.Collections.Generic;

namespace Semgus.Util {
    /// <summary>
    /// Dictionary of lists.
    /// </summary>
    public class DictOfList<TKey, TValue> : DictOfCollection<TKey, List<TValue>, TValue> {
        public DictOfList() : base(_ => new List<TValue>()) { }
    }
}
