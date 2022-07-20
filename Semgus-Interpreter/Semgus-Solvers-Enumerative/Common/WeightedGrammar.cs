using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semgus.Solvers {
    public class WeightedGrammar {
        public WeightedRuleGroup LeafRules { get; }
        public WeightedRuleGroup BranchRules { get; }

        public WeightedGrammar(WeightedRuleGroup leafRules, WeightedRuleGroup branchRules) {
            LeafRules = leafRules;
            BranchRules = branchRules;
        }

        public static WeightedGrammar SizeBased(InterpretationGrammar grammar) {
            if (grammar.PassthroughProductions.Any(p => p.Value.Count > 0)) throw new NotImplementedException();

            var leafRules = new DictOfList<int, NonterminalProduction>();
            var branchRules = new DictOfList<int, NonterminalProduction>();

            foreach (var kvp in grammar.Productions) {
                foreach (var rule in kvp.Value) {
                    if (rule.IsLeaf()) {
                        leafRules.Add(1, rule);
                    } else {
                        branchRules.Add(1, rule);
                    }
                }
            }

            return new WeightedGrammar(new WeightedRuleGroup(leafRules), new WeightedRuleGroup(branchRules));
        }

        public IReadOnlyDictionary<string, int> ToPrettyDict() {
            var dict = new Dictionary<string, int>();
            foreach(var kvp in LeafRules.Concat(BranchRules)) {
                foreach (var rule in kvp.Value) {
                    dict.Add(rule.ToString(), kvp.Key);
                }
            }
            return dict;
        }

        public string PrettyPrint() {
            var sb = new StringBuilder();
            sb.AppendLine("Leaf: {");
            foreach(var kvp in LeafRules) {
                foreach(var rule in kvp.Value) {
                    sb.AppendLine($"  [{kvp.Key}] {rule.ToString()}");
                }
            }
            sb.AppendLine("},");
            sb.AppendLine("Branch: {");
            foreach (var kvp in BranchRules) {
                foreach (var rule in kvp.Value) {
                    sb.AppendLine($"  [{kvp.Key}] {rule.ToString()}");
                }
            }
            sb.AppendLine("},");
            return sb.ToString();
        }
    }
}