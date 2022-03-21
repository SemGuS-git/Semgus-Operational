using System.Collections.Generic;

namespace Semgus.Util {
    /// <summary>
    /// Data structure that associates a unique sequence of elements with a "payload" item.
    /// Can be used like a <see cref="Dictionary{IEnumerable{TSeqElement}, TPayload}"/> (partially).
    /// </summary>
    /// <typeparam name="TSeqElement"></typeparam>
    /// <typeparam name="TPayload"></typeparam>
    public class SequenceHashTreeMap<TSeqElement, TPayload> {
        private class Node {
            private readonly Dictionary<TSeqElement, Node> _dict = new Dictionary<TSeqElement, Node>();
            private bool _seenAsLeaf = false;
            private TPayload _leafPayload = default;

            // If the enumerator is exhausted, return true iff this node hasn't been a leaf before;
            // If the enumerator's next element is new, add the rest of the sequence and return true;
            // Else continue recursively in the existing child node.
            public bool TryIncludeNew(IEnumerator<TSeqElement> enumerator, TPayload payload) {
                if (!enumerator.MoveNext()) {
                    if (_seenAsLeaf) {
                        return false;
                    } else {
                        _seenAsLeaf = true;
                        _leafPayload = payload;
                        return true;
                    }
                }

                var val = enumerator.Current;

                if (_dict.TryGetValue(val, out var node)) {
                    return node.TryIncludeNew(enumerator,payload);
                } else {
                    node = new Node();
                    _dict.Add(val, node);
                    node.AddRecursive(enumerator,payload);
                    return true;
                }
            }

            private void AddRecursive(IEnumerator<TSeqElement> enumerator, TPayload payload) {
                if (!enumerator.MoveNext()) {
                    _seenAsLeaf = true;
                    _leafPayload = payload;
                    return;
                }
                var node = new Node();
                _dict.Add(enumerator.Current, node);
                node.AddRecursive(enumerator, payload);
            }

            public IEnumerable<(IReadOnlyList<TSeqElement>, TPayload)> EnumerateSequences() {
                return EnumerateSequencesInner(new List<TSeqElement>());
            }

            private IEnumerable<(IReadOnlyList<TSeqElement>, TPayload)> EnumerateSequencesInner(List<TSeqElement> currentPath) {
                if(_seenAsLeaf) {
                    yield return (currentPath, _leafPayload);
                }

                var n = currentPath.Count;

                foreach(var kvp in _dict) {
                    if(currentPath.Count > n) {
                        currentPath.RemoveRange(n, currentPath.Count - n);
                    }

                    currentPath.Add(kvp.Key);

                    foreach(var seq in kvp.Value.EnumerateSequencesInner(currentPath)) {
                        yield return seq;
                    }
                }
            }
        }

        private Node _root = new Node();

        public IEnumerable<(IReadOnlyList<TSeqElement> sequence, TPayload payload)> EnumerateSequences() {
            return _root.EnumerateSequences();
        }

        public bool TryAdd(IEnumerable<TSeqElement> sequence, TPayload payload) {
            return _root.TryIncludeNew(sequence.GetEnumerator(), payload);
        }

        public void Clear() {
            _root = new Node();
        }
    }
}
