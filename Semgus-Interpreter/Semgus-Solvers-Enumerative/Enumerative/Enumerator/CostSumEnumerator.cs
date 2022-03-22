using System.Collections.Generic;
using System.Linq;
using Semgus.Operational;
using Semgus.Util;

namespace Semgus.Solvers.Enumerative {
    public class CostSumEnumerator : ITermEnumerator {
        private readonly INodeFactory _nodeFactory;
        private readonly WeightedGrammar _grammar;
        private readonly ExpressionBank _expressionBank;

        public CostSumEnumerator(INodeFactory nodeFactory, WeightedGrammar grammar, ExpressionBank expressionBank) {
            _nodeFactory = nodeFactory;
            _grammar = grammar;
            _expressionBank = expressionBank;
        }

        public IEnumerable<IDSLSyntaxNode> EnumerateAtCost(int budget) {
            if (_grammar.LeafRules.TryGetValue(budget, out var leavesAtCost)) {
                foreach (var leaf in leavesAtCost) {
                    yield return _nodeFactory.Instantiate(leaf.ParentNonterminal, leaf.Production);
                }
            }

            foreach (var kvp in _grammar.BranchRules) {
                var ruleCost = kvp.Key;
                if (ruleCost > budget) continue;

                var childBudget = budget - ruleCost;
                foreach (var rule in kvp.Value) {
                    foreach (var expr in FillAtChildCost(rule, childBudget)) {
                        yield return expr;
                    }
                }
            }
        }

        public IEnumerable<IDSLSyntaxNode> FillAtChildCost(NonterminalProduction rule, int childBudget) {
            var candidateSetsPerSlot = _expressionBank.GetCandidateSets(rule.ChildNonterminals);
            var arity = rule.ChildNonterminals.Count;

            var costsPerSlot = new IEnumerable<int>[arity];
            for (int i = 0; i < arity; i++) {
                costsPerSlot[i] = candidateSetsPerSlot[i].Keys;
            }

            var selection = new List<IDSLSyntaxNode>[arity];

            foreach (var indexVector in IterationUtil.EnumerateChoicesWithSum(costsPerSlot, childBudget)) {

                for (int slotIndex = 0; slotIndex < arity; slotIndex++) {
                    selection[slotIndex] = candidateSetsPerSlot[slotIndex][indexVector[slotIndex]];
                }

                foreach (var choice in IterationUtil.CartesianProduct(selection)) {
                    // Copy choice to prevent errors due to its contents being overwritten
                    // while it is used as a node member.
                    // TODO: move this operation further down the line? It isn't necessary
                    // to have distinct arrays during equiv checking etc.
                    var a = new IDSLSyntaxNode[arity];
                    choice.CopyTo(a, 0);
                    yield return _nodeFactory.Instantiate(rule.ParentNonterminal, rule.Production, a);
                }
            }
        }

        public int GetHighestAvailableCost() {
            int max = 0;

            if(_grammar.LeafRules.Count > 0) {
                max = _grammar.LeafRules.Keys.Max();
            }

            foreach (var kvp in _grammar.BranchRules) {
                var ruleCost = kvp.Key;

                foreach (var rule in kvp.Value) {
                    var candidateSetsPerSlot = _expressionBank.GetCandidateSets(rule.ChildNonterminals);

                    if (candidateSetsPerSlot.Any(cs => cs.Count == 0)) continue; // can't fill this rule

                    var sum = ruleCost;
                    foreach(var candidateSet in candidateSetsPerSlot) {
                        sum += candidateSet.Keys.Max();
                    }

                    if (max < sum) max = sum;
                }
            }

            return max;
        }
    }
}