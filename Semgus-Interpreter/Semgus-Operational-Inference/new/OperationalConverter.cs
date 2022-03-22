using System;
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

namespace Semgus {
    public static class OperationalConverter
    {
        public static InterpretationGrammar ProcessGrammar(SemgusGrammar g, InterpretationLibrary lib) {
            HashSet<NtSymbol> nonterminals = new(g.NonTerminals.Select(a => a.Convert()));

            DictOfList<NtSymbol, NonterminalProduction> dict = new();

            foreach (var nt in g.NonTerminals) {
                dict.AddCollection(nt.Convert(), new());
            }

            foreach(var prod in g.Productions) {
                if(!lib.TryFind(prod.Instance.Sort,prod.Constructor,out var interp)) {
                    throw new KeyNotFoundException("Unable to find production");
                }
                var nt = prod.Instance.Convert();
                dict.Add(nt, new(interp, nt, prod.Occurrences.Select(a => a!.Convert()).ToList()));
            }

            return new(dict);
        }

        public static ITheoryImplementation MapTheory(ISmtTheory theory) {
            return theory switch {
                SmtCoreTheory => SmtCoreTheoryImpl.Instance,
                SmtIntsTheory => SmtIntsTheoryImpl.Instance,
                SmtStringsTheory => SmtStringsTheoryImpl.Instance,
                _ => throw new NotImplementedException(),
            };
        }

        public static InterpretationLibrary ProcessProductions(IEnumerable<ISmtTheory> theories, IEnumerable<SemgusChc> chcs) {
            var theory = new UnionTheoryImpl(theories.Select(MapTheory));

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
                    prod = new(binder.ParentType, binder.Constructor, variables.Inputs.ToList(), variables.Outputs.ToList());
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
