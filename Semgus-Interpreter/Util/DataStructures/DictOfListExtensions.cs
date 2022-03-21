using System;

namespace Semgus.Util {
    public static class DictOfListExtensions {
        public static (DictOfList<TKey,TValue>, DictOfList<TKey,TValue>) Partition<TKey,TValue>(this DictOfList<TKey,TValue> collection, Func<TValue,bool> discriminator) {
            DictOfList<TKey, TValue> a = new(), b = new();

            foreach(var kvp in collection) {
                foreach(var item in kvp.Value) {
                    (discriminator(item) ? a : b).Add(kvp.Key, item);
                }
            }

            return (a, b);
        }
    }
}
