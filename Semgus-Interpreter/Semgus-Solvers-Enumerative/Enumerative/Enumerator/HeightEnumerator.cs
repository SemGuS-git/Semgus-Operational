using Semgus.Operational;
using Semgus.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Semgus.Solvers.Enumerative {
    public class HeightEnumerator : ITermEnumerator {
        private readonly INodeFactory _nodeFactory;
        private readonly ExpressionBank _expressionBank;
        private readonly DictOfList<NtSymbol, NonterminalProduction> _leafTerms;
        private readonly DictOfList<NtSymbol, NonterminalProduction> _branchTerms;

        public HeightEnumerator(INodeFactory nodeFactory, InterpretationGrammar grammar, ExpressionBank expressionBank) {
            if (grammar.PassthroughProductions.Any(p => p.Value.Count > 0)) throw new NotImplementedException();
            _nodeFactory = nodeFactory;
            (_leafTerms, _branchTerms) = grammar.Productions.Partition(p => p.IsLeaf());
            _expressionBank = expressionBank;
        }

        // kludge
        public IReadOnlyDictionary<string, HashSet<NtSymbol>> GetTermTypeToNtMap() {
            var map = new DictOfCollection<string, HashSet<NtSymbol>, NtSymbol>();

            foreach (var kvp in _leafTerms.Concat(_branchTerms)) {
                var tt = kvp.Value[0].Production.TermType.Name.AsString();
                map.SafeGetCollection(tt).Add(kvp.Key);
            }
            return map;
        }
        public IEnumerable<IDSLSyntaxNode> EnumerateAtCost(int height) {
            if (height < 0) throw new ArgumentOutOfRangeException();

            if (height == 0) {
                foreach (var (_, rule) in _leafTerms.EnumerateKeyElementTuples()) {
                    yield return _nodeFactory.Instantiate(rule.ParentNonterminal, rule.Production);
                }
            } else {
                foreach (var (nt, rule) in _branchTerms.EnumerateKeyElementTuples()) {
                    foreach (var expr in FillAtChildHeight(rule, height - 1)) {
                        yield return expr;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate all syntax trees of height <paramref name="maxChildHeight"/> + 1 that can be constructed by instantiating <paramref name="rule"/>
        /// with terms from the current expression bank.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="maxChildHeight"></param>
        /// <returns></returns>
        public IEnumerable<IDSLSyntaxNode> FillAtChildHeight(NonterminalProduction rule, int maxChildHeight) {
            var candidateSetsPerSlot = _expressionBank.GetCandidateSets(rule.ChildNonterminals);
            var arity = rule.ChildNonterminals.Count;

            var selection = new IReadOnlyCollection<IDSLSyntaxNode>[arity];

            foreach (var indexVector in IterationUtil.EnumerateChoicesWithMax(arity, maxChildHeight)) {

                bool missing = false;
                for (int slotIndex = 0; slotIndex < arity; slotIndex++) {
                    if (candidateSetsPerSlot[slotIndex].TryGetValue(indexVector[slotIndex], out var candidateSet)) {
                        selection[slotIndex] = candidateSet;
                    } else {
                        missing = true;
                        break;
                    }
                }
                if (missing) continue;

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
            int max = _leafTerms.ValueCount > 0 ? 1 : 0;

            foreach (var (nt, rule) in _leafTerms.EnumerateKeyElementTuples()) {
                var candidateSetsPerSlot = _expressionBank.GetCandidateSets(rule.ChildNonterminals);

                if (candidateSetsPerSlot.Any(cs => cs.Count == 0)) continue; // can't fill this rule

                var highest = 1+candidateSetsPerSlot.SelectMany(kvp => kvp.Keys).Max();

                if (max < highest) max = highest;
            }

            return max;
        }
    }
}