using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;
using Semgus.TheoryImplementation;

namespace Semgus.Operational {

    public class SmtCoreTheoryImpl : TemplateBasedTheoryImpl {
        private static bool AllSortsMatch(SmtFunctionRank rank, SmtSortIdentifier return_sort_id) => rank.ReturnSort.Name == return_sort_id && rank.ArgumentSorts.All(sort => sort.Name == return_sort_id);

        public SmtCoreTheoryImpl(ISortHelper sortHelper) : base(MakeTemplates(sortHelper)) { }

        private static FunctionTemplate[] MakeTemplates(ISortHelper sortHelper) {
            if (!sortHelper.TryGetSort(SmtCommonIdentifiers.BoolSortId,out var boolSort)) throw new KeyNotFoundException();

            return new FunctionTemplate[] {
            new (
                SmtCommonIdentifiers.AndFunctionId,
                _ => boolSort,
                rank => AllSortsMatch(rank,boolSort.Name),
                rank => args => {
                    for(int i = 0; i < args.Length;i++) {
                        if(!(bool) args[i]) return false;
                    }
                    return true;
                }
            ),
            new (
                SmtCommonIdentifiers.OrFunctionId,
                _ => boolSort,
                rank => AllSortsMatch(rank,boolSort.Name),
                rank => args => {
                        for(int i = 0; i < args.Length;i++) {
                            if((bool) args[i]) return true;
                        }
                        return false;
                }
            ),
            new (
                new("!"),
                _ => boolSort,
                rank => rank.Arity == 1 && AllSortsMatch(rank,boolSort.Name),
                rank => args => !(bool)args[0]
            ),
            new (
                new("not"),
                _ => boolSort,
                rank => rank.Arity == 1 && AllSortsMatch(rank,boolSort.Name),
                rank => args => !(bool)args[0]
            ),
            new (
                new("xor"),
                _ => boolSort,
                rank => rank.Arity >= 1 && AllSortsMatch(rank,boolSort.Name),
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
                _ => boolSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank,boolSort.Name),
                rank => args => (!(bool)args[0])||((bool)args[1])
            ),
            // Polymorphic equality
            new (
                SmtCommonIdentifiers.EqFunctionId,
                argSorts => argSorts.DistinctBy(s=>s.Name.AsString()).SingleOrDefault(),
                rank => rank.ReturnSort.Name == boolSort.Name && rank.Arity >= 2 && rank.ArgumentSorts.Skip(1).All(sort => sort == rank.ArgumentSorts[0]),
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
                rank => rank.ReturnSort.Name == boolSort.Name && rank.Arity == 2 && rank.ArgumentSorts[0] == rank.ArgumentSorts[1],
                rank => args => !args[0].Equals(args[1])
            ),
            // Polymorphic expression ITE
            new (
                new("ite"),
                argSorts => argSorts.Count == 3 ? argSorts.Skip(1).DistinctBy(s=>s.Name.AsString()).SingleOrDefault() : null,
                rank => rank.Arity == 3 && rank.ArgumentSorts[0].Name == boolSort.Name && rank.ArgumentSorts[1] == rank.ReturnSort && rank.ArgumentSorts[2] == rank.ReturnSort,
                rank => args =>  ((bool) args[0] )? args[1] : args[2]
            ),
            // Boolean literals (treated as 0-ary functions)
            new (new("true"), _=>boolSort, rank => rank.Arity == 0 && rank.ReturnSort.Name == boolSort.Name, rank=>args=>true),
            new (new("false"), _=>boolSort, rank => rank.Arity == 0 && rank.ReturnSort.Name == boolSort.Name, rank=>args=>false),
            // Utility functions
            // TODO move somewhere else
            //new (new("just"), argSorts=>argSorts.SingleOrDefault(), rank=>rank.Arity == 1 && rank.ReturnSort == rank.ArgumentSorts[0], rank => args => args[0]),
            //new (new("throw"), rank => true, rank => args => throw new InvalidOperationException("DSL program error")),
        };
        }
    }
}