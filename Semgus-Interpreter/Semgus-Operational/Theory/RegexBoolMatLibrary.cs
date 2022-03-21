//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Semgus.Interpreter {
//    public static class RegexBoolMatLibrary {
//        public static Theory Instance { get; } = MakeInstance();

//        private static Theory MakeInstance() {
//            var functions = new[] {
//                // and
//                // or

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

//                // =
//                // <
//                // <=

//                // TODO: this should require all args to be of the same type (generic)
//                new SmtLibFunction(BasicLibrary.NAME_EQ, "object object* -> bool",
//                    args => {
//                        var t0 = args[0];
//                        for (int i = 1; i < args.Length; i++) {
//                            if (!t0.Equals(args[i])) return false;
//                        }
//                        return true;
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

//                // +
//                // -
//                // div
//                // mod

//                new SmtLibFunction("+", "int int -> int",
//                    args => (int)args[0] + (int)args[1]
//                ),
//                new SmtLibFunction("-", "int int -> int",
//                    args => (int)args[0] - (int)args[1]
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
                

//                // str.len 
//                // bmat.zero
//                // bmat.identity
//                // bmat.any
//                // bmat.plus
//                // bmat.matmul
//                // bmat.get
                
//                new SmtLibFunction("str.len", "string -> int",
//                    args => ((string)args[0]).Length
//                ),
//                new SmtLibFunction("bmat.zero", "string -> BoolMat",
//                    args => BoolMat.Zero((string)args[0])
//                ),
//                new SmtLibFunction("bmat.identity", "string  -> BoolMat",
//                    args => BoolMat.Identity((string)args[0])
//                ),
//                new SmtLibFunction("bmat.any", "string  -> BoolMat",
//                    args => BoolMat.Any((string)args[0])
//                ),
//                new SmtLibFunction("bmat.char", "string string -> BoolMat",
//                    args => BoolMat.Char((string)args[0],(string) args[1])
//                ),
//                new SmtLibFunction("bmat.plus", "BoolMat BoolMat -> BoolMat",
//                    args => BoolMat.Add((BoolMat)args[0], (BoolMat)args[1])
//                ),
//                new SmtLibFunction("bmat.matmul", "BoolMat BoolMat -> BoolMat",
//                    args => BoolMat.MatMul((BoolMat)args[0], (BoolMat)args[1])
//                ),
//                new SmtLibFunction("bmat.get", "BoolMat int int -> bool",
//                    args => ((BoolMat) args[0])[(int)args[1], (int) args[2]]
//                ),
//            }.ToDictionary(fn => fn.Name, fn => fn);
//            return new Theory(
//                identifier: "RegexBoolMat",
//                functions: functions,
//                constants: new Dictionary<string, object> { { "true", true }, { "false", false } },
//                typeMap: new Dictionary<string, Type> {
//                    {"Int", typeof(int)},
//                    {"Bool", typeof(bool)},
//                    {"String", typeof(string)},
//                    {"BoolMat", typeof(BoolMat)},
//                }
//            );

//        }

//    }
//}