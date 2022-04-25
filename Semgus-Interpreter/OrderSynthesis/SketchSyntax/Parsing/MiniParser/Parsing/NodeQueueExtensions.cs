using Semgus.OrderSynthesis.SketchSyntax;
using System.Diagnostics;

namespace Semgus.MiniParser {
    internal static class NodeQueueExtensions {
        internal static T Take<T>(this Queue<ISyntaxNode> q) where T : ISyntaxNode {
            return (T)q.Dequeue();
        }

        internal static bool TryTakeMappedKeyword<T>(this Queue<ISyntaxNode> q, Dictionary<string, T> dict,out T value) {
            if (q.TryPeek(out var a) && a is KeywordInstance key && dict.TryGetValue(key.Value, out value)) {
                q.Dequeue();
                return true;
            }
            value = default(T);
            return false;
        }

        internal static T TakeMappedKeyword<T>(this Queue<ISyntaxNode> q, Dictionary<string, T> dict)
            => TryTakeMappedKeyword(q, dict, out var value) ? value : throw new KeyNotFoundException();


        internal static Queue<ISyntaxNode> Skip<T>(this Queue<ISyntaxNode> q) where T:ISyntaxNode {
            if (q.TryDequeue(out var a) && a is T) return q;
            throw new Exception();
        }

        internal static bool TrySkip<T>(this Queue<ISyntaxNode> q) where T : ISyntaxNode {
            if (!q.TryPeek(out var a) || a is not T) return false;
            q.Dequeue();
            return true;
        }

        internal static Queue<ISyntaxNode> SkipKeyword(this Queue<ISyntaxNode> q, string s) {
            if (q.TryDequeue(out var a) && a is KeywordInstance key && key.Value==s) return q;
            throw new Exception();
        }

        internal static bool TrySkipKeyword(this Queue<ISyntaxNode> q, string s) { 
            if (!q.TryPeek(out var a) || a is not KeywordInstance key || key.Value != s) return false;
            q.Dequeue();
            return true;
        }

        internal static bool TryTake<T>(this Queue<ISyntaxNode> q, out T value) where T : ISyntaxNode {
            if (q.TryPeek(out var a) && a is T aa) {
                value = aa;
                q.Dequeue();
                return true;
            }
            value = default;
            return false;
        }

        internal static IReadOnlyList<T> TakeStar<T>(this Queue<ISyntaxNode> q) where T : ISyntaxNode => q.TakeAtLeast<T>(0);

        internal static IReadOnlyList<T> TakeAtLeast<T>(this Queue<ISyntaxNode> q, int n) where T : ISyntaxNode {
            List<T> list = new List<T>();
            while (q.TryPeek(out var obj) && obj is T va) {
                list.Add(va);
                q.Dequeue();
            }
            Debug.Assert(list.Count >= n);
            return list;
        }
        internal static void AddRange<T>(this Queue<T> q, IEnumerable<T> values) where T : ISyntaxNode {
            foreach (var value in values) q.Enqueue(value);
        }
    }
}
