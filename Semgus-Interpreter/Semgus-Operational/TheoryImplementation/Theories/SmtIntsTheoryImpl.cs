using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;
using Semgus.TheoryImplementation;
using IntegerValue = System.Int64;

namespace Semgus.Operational {
    public class SmtIntsTheoryImpl : TemplateBasedTheoryImpl {
        public static readonly Type IntegerType = typeof(IntegerValue);

        private static bool AllSortsMatch(SmtFunctionRank rank, SmtSortIdentifier return_sort_id) => rank.ReturnSort.Name == return_sort_id && rank.ArgumentSorts.All(sort => sort.Name == return_sort_id);
        private static bool IsIntCmp(SmtFunctionRank rank) => rank.Arity == 2 && rank.ReturnSort.Name == SmtCommonIdentifiers.BoolSortId && rank.ArgumentSorts.All(sort => sort.Name == SmtCommonIdentifiers.IntSortId);

        // Integer division (with positive remainder)
        // Rounding down, not toward zero
        public static IntegerValue IntDiv(IntegerValue a, IntegerValue b) {
            if (b == 0) throw new DivideByZeroException();

            var q = Math.DivRem(a, b, out var r);

            return r < 0 ? q - Math.Sign(b) : q;
        }

        // Integer modulus (always nonnegative)
        public static IntegerValue IntMod(IntegerValue a, IntegerValue b) {
            if (b == 0) throw new DivideByZeroException();

            // C# remainder (toward-zero)
            var r = a % b;

            // Ensure nonnegativity
            return r < 0 ? r + Math.Abs(b) : r;
        }

        public SmtIntsTheoryImpl(ISortHelper sortHelper) : base(MakeTemplates(sortHelper)) { }

        private static FunctionTemplate[] MakeTemplates(ISortHelper sortHelper) {
            if (!sortHelper.TryGetSort(SmtCommonIdentifiers.BoolSortId, out var boolSort)) throw new KeyNotFoundException();
            if (!sortHelper.TryGetSort(SmtCommonIdentifiers.IntSortId, out var intSort)) throw new KeyNotFoundException();
            return new FunctionTemplate[] {
            new (
                new("+"),
                _=>intSort,
                rank => rank.Arity >= 1 && AllSortsMatch(rank, intSort.Name),
                rank => args => {
                    var a = (IntegerValue) args[0];
                    for(int i = 1; i < args.Length; i++) {
                        a += (IntegerValue) args[i];
                    }
                    return a;
                }
            ),
            // negation
            new (
                new("-"),
                _=>intSort,
                rank => rank.Arity == 1 && AllSortsMatch(rank, intSort.Name),
                rank => args => -(IntegerValue)args[0]
            ),
            // subtraction
            new (
                new("-"),
                _=>intSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank, intSort.Name),
                rank => args => (IntegerValue)args[0] - (IntegerValue) args[1]
            ),
            new (
                new("*"),
                _=>intSort,
                rank => rank.Arity >= 1 && AllSortsMatch(rank, intSort.Name),
                rank => args => {
                    var a = (IntegerValue) args[0];
                    for(int i = 1; i < args.Length;i++) {
                        a *= (IntegerValue) args[i];
                    }
                    return a;
                }
            ),
            // Integer div, mod, rem (see https://smtlib.cs.uiowa.edu/theories-Ints.shtml)
            // Integer division (rounding down, not toward zero)
            new (
                new("div"),
                _=>intSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank, intSort.Name),
                rank => args => {
                    var a0 = (IntegerValue) args[0];
                    var a1 = (IntegerValue) args[1];
                    if(a1 == 0) {
#if DIV_ZERO_IS_ZERO
                        return 0;
#else
                        throw new DivideByZeroException();
#endif
                    } else {
                        return IntDiv(a0,a1);
                    }
                }
            ),
            new (
                new("mod"),
                _=>intSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank, intSort.Name),
                rank => args => {
                    var a0 = (IntegerValue) args[0];
                    var a1 = (IntegerValue) args[1];
                    if(a1 == 0) {
#if DIV_ZERO_IS_ZERO
                        return 0;
#else
                        throw new DivideByZeroException();
#endif
                    } else {
                        return IntMod(a0,a1);
                    }
                }
            ),
            // Integer remainder, equal to sign(b) * abs(mod(a,b)) (see https://cs.nyu.edu/pipermail/smt-lib/2014/000823.html)
            new (
                new("rem"),
                _=>intSort,
                rank => rank.Arity == 2 && AllSortsMatch(rank, intSort.Name),
                rank => args => {
                    var a0 = (IntegerValue) args[0];
                    var a1 = (IntegerValue) args[1];
                    if(a1 == 0) {
#if DIV_ZERO_IS_ZERO
                        return 0;
#else
                        throw new DivideByZeroException();
#endif
                    } else {
                        var m = IntMod(a0,a1);
                        return a1 < 0 ? -m : m;
                    }
                }
            ),
            // Integer comparsions
            new (new("<"), _=>boolSort, IsIntCmp, rank => args => ((IntegerValue)args[0])<((IntegerValue)args[1])),
            new (new(">"), _=>boolSort, IsIntCmp, rank => args => ((IntegerValue)args[0])>((IntegerValue)args[1])),
            new (new("<="), _=>boolSort, IsIntCmp, rank => args => ((IntegerValue)args[0])<=((IntegerValue)args[1])),
            new (new(">="), _=>boolSort, IsIntCmp, rank => args => ((IntegerValue)args[0])>=((IntegerValue)args[1])),
        };
        }
    }
}