using Semgus.Model.Smt.Terms;
using Semgus.Model.Smt;
using Semgus.Util;

namespace Semgus.Operational {
    public static class TheoryImplementationExtensions {
        public static object EvalConstant(this ITheoryImplementation theory, SmtTerm term) {
            switch (term) {
                case SmtFunctionApplication fa:
                    if (!theory.TryGetFunction((SmtFunction) fa.Definition, fa.Rank, out var fn)) throw new KeyNotFoundException();
                    var args = new object[fa.Rank.Arity];
                    for (int i = 0; i < args.Length; i++) {
                        args[i] = EvalConstant(theory, fa.Arguments[i]);
                    }
                    return fn.Evaluate(args);
                case SmtLiteral lit:
                    return lit.BoxedValue;
                default:
                    throw new ArgumentException("Expression is non-constant");
            }
        }
        public static object? EvalConstant(this ITheoryImplementation theory, SmtAttributeValue val) => theory.EvalConstantWithSort(val)?.Item2;

        public static (SmtSort, object)? EvalConstantWithSort(this ITheoryImplementation theory, SmtAttributeValue val) {

            switch (val.Type) {
                case SmtAttributeValue.AttributeType.List:
                    var list = val.ListValue!;
                    if(list.Count == 0 || list[0].Type != SmtAttributeValue.AttributeType.Identifier) {
                        throw new ArgumentException("Empty list");
                    }

                    var ctor = list[0].IdentifierValue!;

                    var n = list.Count - 1;

                    var sorts = new SmtSort[n];
                    var vals = new object[n];

                    for(int i = 1; i < list.Count; i++) {
                        (sorts[i], vals[i]) = theory.EvalConstantWithSort(list[i]) ?? throw new ArgumentException("Undetermined values may only occur at root level");
                    }

                    if(!theory.TryGetFunction(ctor,sorts,out var returnSort, out var fn)) {
                        throw new KeyNotFoundException();
                    }

                    return (returnSort, fn.Evaluate(vals));

                case SmtAttributeValue.AttributeType.Identifier:
                    if(!theory.TryGetFunction(val.IdentifierValue!,EmptyCollection<SmtSort>.Instance,out var returnSort1, out var fn1)) {
                        throw new KeyNotFoundException();
                    }
                    return (returnSort1, fn1.Evaluate(Array.Empty<object>()));


                case SmtAttributeValue.AttributeType.Literal:
                    return (val.LiteralValue.Sort, val.LiteralValue!.BoxedValue);
                case SmtAttributeValue.AttributeType.Keyword:
                    if (val.KeywordValue!.Name == "any") {
                        return null;
                    } else {
                        throw new ArgumentException("Unexpected keyword");
                    }
                default:
                    throw new ArgumentException("Expression is non-constant");
            }
        }

    }
}
