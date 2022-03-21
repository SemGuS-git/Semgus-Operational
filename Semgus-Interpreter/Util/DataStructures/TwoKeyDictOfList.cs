namespace Semgus.Util {
    public class TwoKeyDictOfList<TKey0, TKey1, TValue> {
        private readonly AutoDict<TKey0, DictOfList<TKey1, TValue>> _dict = new AutoDict<TKey0, DictOfList<TKey1, TValue>>(_ => new DictOfList<TKey1, TValue>());

        public void Add(TKey0 key0, TKey1 key1, TValue value) {
            _dict.SafeGet(key0).SafeGetCollection(key1).Add(value);
        }

        public DictOfList<TKey1, TValue> SafeGetInner(TKey0 key0) => _dict.SafeGet(key0);
    }

}
