using Semgus.Model;
using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;

namespace Semgus.Solvers.Enumerative {
    public class ObservationalEquivalenceCache : AutoDict<string, SequenceHashTreeMap<object, IDSLSyntaxNode>> {
        public ObservationalEquivalenceCache() : base(_ => new SequenceHashTreeMap<object, IDSLSyntaxNode>()) { }

        public class CacheEntry {
            public string TermTypeName { get; set; }
            public object[,] Outputs { get; set; }
            public IDSLSyntaxNode Expression { get; set; }

            public CacheEntry(string termTypeName, object[,] outputs, IDSLSyntaxNode expression) {
                this.TermTypeName = termTypeName;
                this.Outputs = outputs;
                this.Expression = expression;
            }
        }

        public IEnumerable<CacheEntry> GetEntries() {
            foreach (var kvp in this) {
                foreach (var (seq, expression) in kvp.Value.EnumerateSequences()) {
                    var n_all = seq.Count;
                    var n_outvars = expression.ProductionRule.OutputVariables.Count;

                    if (n_all % n_outvars != 0) throw new IndexOutOfRangeException();

                    var n_examples = n_all / n_outvars;

                    var outputs = new object[n_examples, n_outvars];

                    for (int i = 0; i < n_examples; i++) {
                        for (int j = 0; j < n_outvars; j++) {
                            outputs[i, j] = seq[i * n_outvars + j];
                        }
                    }

                    yield return new CacheEntry(kvp.Key, outputs, expression);
                }
            }
        }
    }
}
