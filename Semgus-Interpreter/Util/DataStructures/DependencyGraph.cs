using System;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Util {
    public static class SortingExtensions {
        /// <summary>
        /// Given a sequence in which all elements are comparable (and some may be equal),
        /// return the earliest element in the sequence that is less than or equal to
        /// all subsequent elements.
        /// </summary>
        public static T FirstMin<T>(this IEnumerable<T> enumerable, IComparer<T> comparer) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) throw new InvalidOperationException();
            var min = enumerator.Current;
            while(enumerator.MoveNext()) {
                var candidate = enumerator.Current;
                if (comparer.Compare(candidate, min) < 0) min = candidate;
            }
            return min;
        }

        /// <summary>
        /// Given a sequence in which all elements are comparable (and some may be equal),
        /// return the earliest element in the sequence that is greater than or equal to
        /// all subsequent elements.
        /// </summary>
        public static T FirstMax<T>(this IEnumerable<T> enumerable, IComparer<T> comparer) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) throw new InvalidOperationException();
            var max = enumerator.Current;
            while (enumerator.MoveNext()) {
                var candidate = enumerator.Current;
                if (comparer.Compare(candidate, max) > 0) max = candidate;
            }
            return max;
        }
    }

    public class DependencyGraph<T> {
        // Map from each node to its dependencies
        private readonly Dictionary<T, IReadOnlyCollection<T>> _dependencyMap = new Dictionary<T, IReadOnlyCollection<T>>();

        /// <summary>
        /// Topological sort.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<T> Sort() {
            var sorted = new List<T>();

            foreach (var node in _dependencyMap.Keys.ToList()) {
                Visit(node, sorted, new HashSet<T>());
            }
            return sorted;

            // Recursive algorithm
            void Visit(T node, List<T> resolved, HashSet<T> entered) {
                if (resolved.Contains(node)) return;
                if (entered.Contains(node)) throw new Exception("Cyclic dependency (mutual constraints are not permitted at this time)");
                entered.Add(node);

                if (_dependencyMap.TryGetValue(node, out var dependencies)) {
                    // Try to resolve all this node's dependencies
                    foreach (var dep in dependencies) {
                        if (!resolved.Contains(dep)) {
                            Visit(dep, resolved, entered);
                        }
                    }
                } else {
                    // Independent node (do nothing)
                }

                // All dependencies are resolved at this point
                resolved.Add(node);
            }
        }
        
        /// <summary>
        /// Topological sort with a lexicographic tiebreaker.
        /// </summary>
        /// <param name="tiebreaker"></param>
        /// <returns></returns>
        public IReadOnlyList<T> Sort(IComparer<T> tiebreaker) {
            // Scratch collections
            var parentToChildren = new DictOfCollection<T, HashSet<T>, T>(_ => new HashSet<T>());
            var childToParents = new DictOfCollection<T, HashSet<T>, T>(_ => new HashSet<T>());
            var independent = new HashSet<T>();

            int nodeCount = 0;

            foreach (var kvp in _dependencyMap) {
                var child = kvp.Key;
                if (kvp.Value.Count == 0) {
                    independent.Add(child);
                } else {
                    childToParents.AddCollection(child, new HashSet<T>(kvp.Value));
                    foreach (var parent in kvp.Value) {
                        parentToChildren.Add(parent, child);

                        // Discover independent nodes
                        if (!_dependencyMap.ContainsKey(parent)) {
                            independent.Add(parent);
                            nodeCount++;
                        }
                    }
                }
                nodeCount++;
            }

            // Kahn's algorithm with tiebreaker
            var sorted = new List<T>();

            while (independent.Count>0) {
                var next = independent.FirstMin(tiebreaker);
                independent.Remove(next);
                sorted.Add(next);

                if (parentToChildren.TryGetValue(next,out var children)) {
                    foreach (var child in children) {
                        var unsortedParents = childToParents[child];
                        unsortedParents.Remove(next);
                        if (unsortedParents.Count == 0) {
                            independent.Add(child);
                        }
                    }
                }
            }

            if (nodeCount != sorted.Count) throw new Exception(); // sanity check

            return sorted;
        }

        public void Add(T node, IReadOnlyCollection<T> dependencies) => _dependencyMap.Add(node, dependencies);
    }
}