﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semgus.Operational;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Model.Smt.Terms;
using Semgus.Model.Smt.Theories;
using Semgus.Util;
using Semgus.TheoryImplementation;
using System.Diagnostics;

namespace Semgus {

    public class SortHelper : ISortHelper {
        private readonly SmtContext _context;

        public SortHelper(SmtContext context) {
            this._context = context;
        }

        public bool TryGetSort(SmtSortIdentifier id, out SmtSort sort) {
            SmtSort found = default;
            bool got = false;
            if(_context.TryGetSortDeclaration(id, out var now))
            {
                found = now;
                got = true;
            }
            sort = got ? found! : default;
            return got;
        }
    }

    public static class OperationalConverter
    {
        public static InterpretationGrammar ProcessGrammar(SemgusGrammar g, InterpretationLibrary lib) {
            HashSet<NtSymbol> nonterminals = new(g.NonTerminals.Select(a => a.Convert()));

            var start_symbol = g.NonTerminals.First().Convert();

            DictOfList<NtSymbol, NonterminalProduction> dict = new();
            DictOfList<NtSymbol, NtSymbol> dict_passthrough = new();

            foreach (var prod in g.Productions) {
                var nt = prod.Instance.Convert();
                Debug.Assert(nonterminals.Contains(nt));

                // Handle case of passthrough productions, e.g. A ::= B
                if (prod.Constructor is null) {
                    if (prod.Occurrences.Count != 1) throw new InvalidDataException($"Invalid production {prod}");
                    var pt = prod.Occurrences[0]!.Convert();
                    dict_passthrough.Add(nt, pt);
                } else {
                    if (!lib.TryFind(prod.Instance.Sort, prod.Constructor, out var interp)) throw new KeyNotFoundException($"Unable to find production {prod.Constructor}");
                    dict.Add(nt, new(interp, nt, prod.Occurrences.Select(a => a!.Convert()).ToList()));
                }
            }

            List<NtSymbol> discards = new();
            foreach (NtSymbol nt in nonterminals) {
                if (dict.ContainsKey(nt)) {
                    dict_passthrough.SafeGetCollection(nt);
                } else if (dict_passthrough.ContainsKey(nt)) {
                    dict.SafeGetCollection(nt);
                } else {
                    discards.Add(nt);
                }
            }

            if (discards.Contains(start_symbol)) throw new InvalidDataException($"Start symbol {start_symbol} has no productions");

            if(discards.Count > 0) {
                // todo log this properly
                Console.WriteLine($"Warning: NT symbol(s) [{string.Join(", ", discards)}] have no productions and will be discarded");
                foreach (var a in discards) nonterminals.Remove(a);
            }

            return new(start_symbol,dict,dict_passthrough);
        }

        public static ITheoryImplementation MapTheory(ISmtTheory theory, ISortHelper sortHelper) {
            return theory.Name switch {
                var a when a == SmtCommonIdentifiers.CoreTheoryId => new SmtCoreTheoryImpl(sortHelper),
                var a when a == SmtCommonIdentifiers.IntsTheoryId => new SmtIntsTheoryImpl(sortHelper),
                var b when b == SmtCommonIdentifiers.StringsTheoryId => new SmtStringsTheoryImpl(sortHelper),
                //var b when b == SmtCommonIdentifiers.BitVectorsTheoryId => new SmtBitVectorsTheoryImpl(sortHelper),
                _ => throw new NotImplementedException(),
            };
        }

        public static InterpretationLibrary ProcessProductions(SmtContext context, IEnumerable<SemgusChc> chcs) {
            var theoriesList = context.Theories.ToList();
            var sortHelper = new SortHelper(context);

            //todo: for now, ignore bitvector theories
            theoriesList.RemoveAt(3);

            var theory = new UnionTheoryImpl(theoriesList.Select(th => MapTheory(th, sortHelper)));

            var relations = new RelationTracker(chcs);

            Dictionary<string,ProductionRuleInterpreter> productions = new();

            foreach(var chc in chcs) {
                var binder = chc.Binder;
                if (binder.Constructor is null) throw new NullReferenceException("CHC missing syntax constructor");

                var key = ToSyntaxKey(binder.ParentType, binder.Constructor);
                var variables = new LocalScopeVariables(chc);

                ProductionRuleInterpreter prod;
                if (productions.TryGetValue(key, out prod!)) {
                    if (!(prod.InputVariables.SequenceEqual(variables.Inputs) && prod.OutputVariables.SequenceEqual(variables.Outputs))) throw new InvalidDataException("Mismatching variable sets for different CHCs under same production");
                } else {
                    prod = new(productions.Count, binder.ParentType, binder.Constructor, variables.Inputs.ToList(), variables.Outputs.ToList());
                    productions.Add(key, prod);
                }
                var helper = new NamespaceContext(theory, relations, new LocalScopeTerms(chc), variables);

                var sem = GetChcSemantics(helper, prod, chc);
                prod.AddSemanticRule(sem);
            }

            return new(theory, relations, productions.Values.ToList());
        }

        static string ToSyntaxKey(SemgusTermType termType, SemgusTermType.Constructor ctor) {
            var sb = new StringBuilder();
            sb.Append(termType.StringName());
            sb.Append(':');
            sb.Append(ctor.Operator.AsString());
            if(ctor.Children.Length>0) {

                sb.Append('(');
                foreach(var child in ctor.Children) {
                    sb.Append(child.Name.AsString());
                    sb.Append(',');
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

        static SemanticRuleInterpreter GetChcSemantics(NamespaceContext helper, ProductionRuleInterpreter prod, SemgusChc chc) {

            StatementOrganizer statements = new();

            foreach(var rel in chc.BodyRelations) {
                statements.Add(helper.MakeTermEvaluation(rel));
            }

            var root = chc.Constraint;
            if (root is not null) {
                var conjunctiveAtoms = new Queue<SmtTerm>();
                conjunctiveAtoms.Enqueue(root!);

                while (conjunctiveAtoms.TryDequeue(out var term)) {
                    if (term is SmtFunctionApplication fa) {
                        if (fa.Definition.IsConjunction()) {
                            foreach (var t in fa.Arguments) conjunctiveAtoms.Enqueue(t);
                            continue;
                        } else if (helper.MakeMaybeSetter(fa,out var step)) {
                            statements.Add(step!);
                            continue;
                        }
                    }
                    statements.Add(helper.MakeAssertion(term));
                }
            }

            var steps = statements.Resolve(helper.Variables);

            return new SemanticRuleInterpreter(prod, helper.Variables.Variables.Where(v=>v.Usage == VariableUsage.Auxiliary).ToList(), steps);
        }
    }
}
