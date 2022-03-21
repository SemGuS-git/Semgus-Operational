//using Semgus.Syntax;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Semgus.Interpreter {
//    public static class SemgusMath {
//        // Integer division (with positive remainder)
//        // Rounding down, not toward zero
//        public static int IntDiv(int a, int b) {
//            if (b == 0) throw new DivideByZeroException();

//            var q = Math.DivRem(a, b, out var r);
//            return r < 0 ? q - Math.Sign(b) : q;
//        }

//        // Integer modulus (always nonnegative)
//        public static int IntMod(int a, int b) {
//            if (b == 0) throw new DivideByZeroException();

//            // C# remainder (toward-zero)
//            var r = a % b;

//            // Ensure nonnegativity
//            return r < 0 ? r + Math.Abs(b) : r;
//        }
//    }

//    public static class BasicLibrary {
//        public const string NAME_EQ = "=";

//        public static Theory Instance { get; } = MakeInstance();

//        private static Theory MakeInstance() {
//            var functions = new[] {
//                new SmtLibFunction("and", "bool bool* -> bool",
//                    args => {
//                        for(int i = 0; i < args.Length; i++) {
//                            if(!((bool) args[i])) return false;
//                        }
//                        return true;
//                    }
//                ),

//                new SmtLibFunction("or", "bool bool* -> bool",
//                    args => {
//                        for(int i = 0; i < args.Length; i++) {
//                            if((bool) args[i]) return true;
//                        }
//                        return false;
//                    }
//                ),

//                new SmtLibFunction("!", "bool -> bool",
//                    args => {
//                        return !((bool)args[0]);
//                    }
//                ),

//                // TODO: this should require all args to be of the same type (generic)
//                new SmtLibFunction(NAME_EQ, "object object* -> bool",
//                    args => {
//                        var t0 = args[0];
//                        for (int i = 1; i < args.Length; i++) {
//                            if (!t0.Equals(args[i])) return false;
//                        }
//                        return true;
//                    }
//                ),

//                new SmtLibFunction("+", "int int* -> int",
//                    args => {
//                        var a = (int)args[0];
//                        for (int i = 1; i < args.Length; i++) {
//                            a += (int) args[i];
//                        }
//                        return a;
//                    }
//                ),

//                // TODO: signature should permit 1 or 2 arguments
//                // (1 for negative literals, 2 for subtraction)
//                new SmtLibFunction("-", "int int* -> int",
//                    args => {
//                        if(args.Length==1) return -(int) args[0];
//                        if(args.Length==2) return (int) args[0] - (int) args[1];
//                        throw new ArgumentOutOfRangeException($"Minus function supports 1 or 2 operands (found {args.Length})");
//                    }
//                ),

//                new SmtLibFunction("*", "int int -> int",
//                    args => {
//                        return (int) args[0] * (int) args[1];
//                    }
//                ),

//                // for div and mod, see https://smtlib.cs.uiowa.edu/theories-Ints.shtml
//                // Integer division (rounding down, not toward zero)
//                new SmtLibFunction("div", "int int -> int",
//                    args => {
//                        var a0 = (int) args[0];
//                        var a1 = (int) args[1];
//                        if(a1 == 0) {
//#if DIV_ZERO_IS_ZERO
//                            return 0;
//#else
//                            throw new DivideByZeroException();
//#endif
//                        } else {
//                            return SemgusMath.IntDiv(a0,a1);
//                        }
//                    }
//                ),
                
//                // Integer modulus (nonnegative)
//                new SmtLibFunction("mod", "int int -> int",
//                    args => {
//                        var a0 = (int) args[0];
//                        var a1 = (int) args[1];
//                        if(a1 == 0) {
//#if DIV_ZERO_IS_ZERO
//                            return 0;
//#else
//                            throw new DivideByZeroException();
//#endif
//                        } else {
//                            return SemgusMath.IntMod(a0,a1);
//                        }
//                    }
//                ),
                
//                // Integer remainder, equal to sign(b) * abs(mod(a,b)) (see https://cs.nyu.edu/pipermail/smt-lib/2014/000823.html)
//                new SmtLibFunction("rem", "int int -> int",
//                    args => {
//                        var a0 = (int) args[0];
//                        var a1 = (int) args[1];
//                        if(a1 == 0) {
//#if DIV_ZERO_IS_ZERO
//                            return 0;
//#else
//                            throw new DivideByZeroException();
//#endif
//                        } else {
//                            var m = SemgusMath.IntMod(a0,a1);
//                            return a1 < 0 ? -m : m;
//                        }
//                    }
//                ),

//                new SmtLibFunction("ite", "bool int int -> int",
//                    args => {
//                        return (bool) args[0] ? (int) args[1] : (int) args[2];
//                    }
//                ),


//                new SmtLibFunction("<", "int int -> bool",
//                    args => {
//                        return (int) args[0] < (int) args[1];
//                    }
//                ),
//                new SmtLibFunction("<=", "int int -> bool",
//                    args => {
//                        return (int) args[0] <= (int) args[1];
//                    }
//                ),

//                new SmtLibFunction("just", "object -> object", args => args[0]),

//                new SmtLibFunction("throw", new SmtLibFunction.TypeSignature(typeof(bool), Array.Empty<Type>()), args => {throw new InvalidOperationException("DSL program error"); }),

//            }.ToDictionary(fn => fn.Name, fn => fn);

//            functions.Add("not", functions["!"]);

//            return new Theory(
//                identifier: "BasicLibrary",
//            functions: functions,
//                constants: new Dictionary<string, object> { { "true", true }, { "false", false } },
//                typeMap: new Dictionary<string, Type> {
//                {"Int", typeof(int)},
//                {"Bool", typeof(bool)},
//            }
//            );
//        }

//    }
//}