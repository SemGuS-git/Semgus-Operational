using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Solvers.Common {
    public static class GrammarCostGraph {
        public static Dictionary<NtSymbol,int> ComputeMinAstSizes(InterpretationGrammar grammar) {
            Dictionary<NtSymbol, int> costs = new();
            UniqueQueue<NonterminalProduction> dirty = new();
            DictOfCollection<NtSymbol, HashSet<NonterminalProduction>, NonterminalProduction> subscriptionMap = new(_=>new());

            foreach (var kvp in grammar.Productions) {
                foreach (var rule in kvp.Value) {
                    if (rule.IsLeaf()) {
                        costs.TryAdd(kvp.Key, 1);
                    } else {
                        dirty.Enqueue(rule); // add all branch nodes to dirty queue
                        foreach (var source in rule.ChildNonterminals) {
                            subscriptionMap.Add(source, rule);
                        }
                    }
                }
            }

            bool TryUpdateCost(NonterminalProduction rule) {
                int a = 1;
                foreach(var childNt in rule.ChildNonterminals) {
                    if(costs.TryGetValue(childNt,out var cost)) {
                        a += cost;
                    } else {
                        return false;
                    }
                }
                var nt = rule.ParentNonterminal;
                if (costs.TryGetValue(nt, out var prev) && prev < a) {
                    return false;
                } else {
                    costs[nt] = a;
                    return true;
                }
            }


            while(dirty.Count>0) {
                var next = dirty.Dequeue();
                if(TryUpdateCost(next)) {
                    if(subscriptionMap.TryGetValue(next.ParentNonterminal,out var group)) {
                        dirty.EnqueueRange(group);
                    }
                }
            }

            if(costs.Keys.Count != grammar.Nonterminals.Count) {
                var exceptions = grammar.Nonterminals.Except(costs.Keys);
                throw new InvalidOperationException($"Unable to compute cost for nonterminals {{{string.Join(", ", exceptions)}}}");
            }

            return costs;
        }
    }
}
