using Semgus.OrderSynthesis.SketchSyntax;
using System.Diagnostics;

namespace Semgus.MiniParser {
    internal static class NodeQueueExtensions {
        internal static T Take<T>(this Queue<INode> q) {
            return (T)q.Dequeue();
        }

        internal static bool TryTakeKeywordFrom<T>(this Queue<INode> q, Dictionary<string, T> dict,out T value) {
            if (q.TryPeek(out var a) && a is KeywordSymbol key && dict.TryGetValue(key.Value, out value)) {
                q.Dequeue();
                return true;
            }
            value = default(T);
            return false;
        }

        internal static T TakeKeywordFrom<T>(this Queue<INode> q, Dictionary<string, T> dict) => TryTakeKeywordFrom(q, dict, out var value) ? value : throw new KeyNotFoundException();


        internal static Queue<INode> Skip<T>(this Queue<INode> q) {
            if (q.TryDequeue(out var a) && a is T) return q;
            throw new Exception();
        }

        internal static bool TrySkip<T>(this Queue<INode> q) {
            if (!q.TryPeek(out var a) || a is not T) return false;
            q.Dequeue();
            return true;
        }

        internal static Queue<INode> SkipKeyword(this Queue<INode> q, string s) {
            if (q.TryDequeue(out var a) && a is KeywordSymbol key && key.Value==s) return q;
            throw new Exception();
        }

        internal static bool TrySkipKeyword(this Queue<INode> q, string s) {
            if (!q.TryPeek(out var a) || a is not KeywordSymbol key || key.Value != s) return false;
            q.Dequeue();
            return true;
        }

        internal static bool TryTake<T>(this Queue<INode> q, out T value) {
            if (q.TryPeek(out var a) && a is T aa) {
                value = aa;
                q.Dequeue();
                return true;
            }
            value = default;
            return false;
        }

        internal static IReadOnlyList<T> TakeStar<T>(this Queue<INode> q) => q.TakeAtLeast<T>(0);

        internal static IReadOnlyList<T> TakeAtLeast<T>(this Queue<INode> q, int n) {
            List<T> list = new List<T>();
            while (q.TryPeek(out var obj) && obj is T va) {
                list.Add(va);
                q.Dequeue();
            }
            Debug.Assert(list.Count >= n);
            return list;
        }
        internal static void AddRange<T>(this Queue<T> q, IEnumerable<T> values) {
            foreach (var value in values) q.Enqueue(value);
        }
    }
}
