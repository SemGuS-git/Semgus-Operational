using System.Collections.Generic;

namespace Semgus.Util {
    public static class ListUtil {
        public static Dictionary<T, int> ToIndexMap<T>(IEnumerable<T> src, int offset = 0) {
            var d = new Dictionary<T, int>();
            int i = 0;
            var e = src.GetEnumerator();
            while (e.MoveNext()) {
                d[e.Current] = i++ + offset;
            }
            return d;
        }
    }
}
