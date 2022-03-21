using Semgus.Model.Smt;
using Semgus.Model.Smt.Theories;

namespace Semgus.Interpretation {
    public class SmtIntsTheoryImpl : TemplateBasedTheoryImpl {
        private static bool AllSortsMatch(SmtFunctionRank rank, SmtIdentifier return_sort_id) => rank.ReturnSort.Name == return_sort_id && rank.ArgumentSorts.All(sort => sort.Name == return_sort_id);
        private static bool IsIntCmp(SmtFunctionRank rank) => rank.Arity == 2 && rank.ReturnSort.Name == SmtCommonIdentifiers.SORT_BOOL && rank.ArgumentSorts.All(sort => sort.Name == SmtCommonIdentifiers.SORT_INT);


        // Integer division (with positive remainder)
        // Rounding down, not toward zero
        public static int IntDiv(int a, int b) {
            if (b == 0) throw new DivideByZeroException();

            var q = Math.DivRem(a, b, out var r);
            return r < 0 ? q - Math.Sign(b) : q;
        }

        // Integer modulus (always nonnegative)
        public static int IntMod(int a, int b) {
            if (b == 0) throw new DivideByZeroException();

            // C# remainder (toward-zero)
            var r = a % b;

            // Ensure nonnegativity
            return r < 0 ? r + Math.Abs(b) : r;
        }

        public static SmtIntsTheoryImpl Instance { get; } = new ();

        private SmtIntsTheoryImpl() : base(TEMPLATES) { }

        private static readonly FunctionTemplate[] TEMPLATES = new FunctionTemplate[] {
            new (
                new("+"),
                rank => rank.Arity >= 1 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => {
                    int a = (int) args[0];
                    for(int i = 1; i < args.Length; i++) {
                        a += (int) args[i];
                    }
                    return a;
                }
            ),
            // negation
            new (
                new("-"),
                rank => rank.Arity == 1 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => -(int)args[0]
            ),
            // subtraction
            new (
                new("-"),
                rank => rank.Arity == 2 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => (int)args[0] - (int) args[1]
            ),
            new (
                new("*"),
                rank => rank.Arity >= 1 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => {
                    int a = (int) args[0];
                    for(int i = 1; i < args.Length;i++) {
                        a *= (int) args[i];
                    }
                    return a;
                }
            ),
            // Integer div, mod, rem (see https://smtlib.cs.uiowa.edu/theories-Ints.shtml)
            // Integer division (rounding down, not toward zero)
            new (
                new("div"),
                rank => rank.Arity == 2 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => {
                    var a0 = (int) args[0];
                    var a1 = (int) args[1];
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
                rank => rank.Arity == 2 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => {
                    var a0 = (int) args[0];
                    var a1 = (int) args[1];
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
                rank => rank.Arity == 2 && AllSortsMatch(rank, SmtCommonIdentifiers.SORT_INT),
                rank => args => {
                    var a0 = (int) args[0];
                    var a1 = (int) args[1];
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
            new (new("<"), IsIntCmp, rank => args => ((int)args[0])<((int)args[1])),
            new (new(">"), IsIntCmp, rank => args => ((int)args[0])>((int)args[1])),
            new (new("<="), IsIntCmp, rank => args => ((int)args[0])<=((int)args[1])),
            new (new(">="), IsIntCmp, rank => args => ((int)args[0])>=((int)args[1])),
        };
    }
}