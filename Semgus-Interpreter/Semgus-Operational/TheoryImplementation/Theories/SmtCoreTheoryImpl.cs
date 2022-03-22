﻿using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;

namespace Semgus.Operational {

    public class SmtCoreTheoryImpl : TemplateBasedTheoryImpl {
        private static bool AllSortsMatch(SmtFunctionRank rank, SmtIdentifier return_sort_id) => rank.ReturnSort.Name == return_sort_id && rank.ArgumentSorts.All(sort => sort.Name == return_sort_id);

        public static SmtCoreTheoryImpl Instance { get; } = new();

        private SmtCoreTheoryImpl() : base(MakeTemplates()) { }

        public static SmtSort BoolSort { get; } = SmtCoreTheory.Instance.Sorts[SmtCommonIdentifiers.SORT_BOOL];

        private static FunctionTemplate[] MakeTemplates() => new FunctionTemplate[] {
            new (
                SmtCommonIdentifiers.FN_AND,
                _ => BoolSort,
                rank => AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => {
                    for(int i = 0; i < args.Length;i++) {
                        if(!(bool) args[i]) return false;
                    }
                    return true;
                }
            ),
            new (
                SmtCommonIdentifiers.FN_OR,
                _ => BoolSort,
                rank => AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => {
                        for(int i = 0; i < args.Length;i++) {
                            if((bool) args[i]) return true;
                        }
                        return false;
                }
            ),
            new (
                new("!"),
                _ => BoolSort,
                rank => rank.Arity == 1 && AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => !(bool)args[0]
            ),
            new (
                new("not"),
                _ => BoolSort,
                rank => rank.Arity == 1 && AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => !(bool)args[0]
            ),
            new (
                new("xor"),
                _ => BoolSort,
                rank => rank.Arity >= 1 && AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => {
                    var any = (bool) args[0];
                    for(int i = 1; i < args.Length;i++) {
                        if((bool)args[i]) {
                            if(any) return false;
                            any = true;
                        }
                    }
                    return any;
                }
            ),
            new (
                new("=>"),
                _ => BoolSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank,SmtCommonIdentifiers.SORT_BOOL),
                rank => args => (!(bool)args[0])||((bool)args[1])
            ),
            // Polymorphic equality
            new (
                SmtCommonIdentifiers.FN_EQ,
                argSorts => argSorts.DistinctBy(s=>s.Name.AsString()).SingleOrDefault(),
                rank => rank.ReturnSort.Name == SmtCommonIdentifiers.SORT_BOOL && rank.Arity >= 2 && rank.ArgumentSorts.Skip(1).All(sort => sort == rank.ArgumentSorts[0]),
                rank => args => {
                    var t0 = args[0];
                    for (int i = 1; i < args.Length; i++) {
                        if (!t0.Equals(args[i])) return false;
                    }
                    return true;
                }
            ),
            // Polymorphic inequality
            new (
                new("distinct"),
                argSorts => argSorts.DistinctBy(s=>s.Name.AsString()).SingleOrDefault(),
                rank => rank.ReturnSort.Name == SmtCommonIdentifiers.SORT_BOOL && rank.Arity == 2 && rank.ArgumentSorts[0] == rank.ArgumentSorts[1],
                rank => args => !args[0].Equals(args[1])
            ),
            // Polymorphic expression ITE
            new (
                new("ite"),
                argSorts => argSorts.Count == 3 ? argSorts.Skip(1).DistinctBy(s=>s.Name.AsString()).SingleOrDefault() : null,
                rank => rank.Arity == 3 && rank.ArgumentSorts[0].Name == SmtCommonIdentifiers.SORT_BOOL && rank.ArgumentSorts[1] == rank.ReturnSort && rank.ArgumentSorts[2] == rank.ReturnSort,
                rank => args =>  ((bool) args[0] )? args[1] : args[2]
            ),
            // Boolean literals (treated as 0-ary functions)
            new (new("true"), _=>BoolSort, rank => rank.Arity == 0 && rank.ReturnSort.Name == SmtCommonIdentifiers.SORT_BOOL, rank=>args=>true),
            new (new("false"), _=>BoolSort, rank => rank.Arity == 0 && rank.ReturnSort.Name == SmtCommonIdentifiers.SORT_BOOL, rank=>args=>false),
            // Utility functions
            new (new("just"), argSorts=>argSorts.SingleOrDefault(), rank=>rank.Arity == 1 && rank.ReturnSort == rank.ArgumentSorts[0], rank => args => args[0]),
            //new (new("throw"), rank => true, rank => args => throw new InvalidOperationException("DSL program error")),
        };
    }
}