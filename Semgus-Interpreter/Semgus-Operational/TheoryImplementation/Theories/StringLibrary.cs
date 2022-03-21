//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace Semgus.Interpreter {

//    /// <summary>
//    /// Implemented to match specification at http://smtlib.cs.uiowa.edu/theories-UnicodeStrings.shtml
//    /// </summary>
//    public static class StringLibrary {
//        public static Theory Instance { get; } = MakeInstance();

//        // http://smtlib.cs.uiowa.edu/theories-UnicodeStrings.shtml
//        private static Theory MakeInstance() {
//            var functions = new[] {
//                // str.++
//                // str.replace
//                // str.at
//                // int.to.str
//                // ite<str>
//                // str.substr

//                new SmtLibFunction("str.++", "string string -> string",
//                    args => string.Concat((string)args[0],(string)args[1])
//                ),

//                new SmtLibFunction("str.replace", "string string string -> string", args => {
//                        var subject = (string) args[0];
//                        var target = (string) args[1];
//                        var replacement = (string) args[2];

//                        // if target is empty, prepend replacement to subject
//                        if(target == string.Empty) return string.Concat(replacement,subject);

//                        var i = subject.IndexOf(target);
//                        if(i == -1) return subject;

//                        // Replace first occurrence of target
//                        return string.Concat(subject.Substring(0,i), replacement, subject.Substring(i+target.Length));
//                    }
//                ),

//                new SmtLibFunction("str.at", "string int -> string",
//                    args => {
//                        var s = (string)args[0];
//                        var i = (int)args[1];
//                        if(0 <= i && i < s.Length) {
//                            return s.Substring(i,1);
//                        } else {
//                            return string.Empty;
//                        }
//                    }
//                ),

//                new SmtLibFunction("int.to.str", "int -> string",
//                    args => ((int)args[0]).ToString()
//                ),


//                new SmtLibFunction("ite", "bool object object -> object",
//                    args => ((bool)args[0]) ? args[1] : args[2]
//                ),

//                new SmtLibFunction("str.substr", "string int int -> string",
//                    args => {
//                        var s = (string) args[0];
//                        var i = (int) args[1];
//                        var n = (int) args[2];

//                        var l = s.Length;

//                        if(n < 1 || i < 0 || i >= l - 1) return "";

//                        if(i + n > l) n = l - i;

//                        return s.Substring(i,n);
//                    }
//                ),

//                // +
//                // -
//                // str.len
//                // str.to.int
//                // ite<int>
//                // str.indexof

//                new SmtLibFunction("+", "int int -> int",
//                    args => (int)args[0] + (int)args[1]
//                ),
//                new SmtLibFunction("-", "int int -> int",
//                    args => (int)args[0] - (int)args[1]
//                ),
//                new SmtLibFunction("str.len", "string -> int",
//                    args => ((string)args[0]).Length
//                ),
//                new SmtLibFunction("str.to.int", "string -> int",
//                    args => {
//                        var s = (string) args[0];
//                        // Require digits only
//                        if(!Regex.IsMatch(s, @"^[0-9]+$")) return -1;
//                        return int.TryParse(s,out var value) ? value : -1;
//                    }),
//                new SmtLibFunction("str.indexof", "string string int -> int",
//                    args => {
//                        var s = (string) args[0];
//                        var t = (string) args[1];
//                        var i = (int) args[2];

//                        if(i < 0 || i > s.Length) return -1;
//                        return s.IndexOf(t,i);
//                    }
//                ),

//                // =<int>
//                // str.prefixof
//                // str.suffixof
//                // str.contains
                
//                new SmtLibFunction("=", "int int -> bool",
//                    args => (int)args[0] == (int) args[1]
//                ),
//                new SmtLibFunction("str.prefixof", "string string -> bool",
//                    args => ((string)args[1]).StartsWith((string)args[0])
//                ),
//                new SmtLibFunction("str.suffixof", "string string -> bool",
//                    args => ((string)args[1]).EndsWith((string)args[0])
//                ),
//                new SmtLibFunction("str.contains", "string string -> bool",
//                    args => ((string)args[0]).Contains((string)args[1])
//                ),
//            }.ToDictionary(fn => fn.Name, fn => fn);

//            return new Theory(
//                identifier: "String",
//                functions: functions,
//                constants: new Dictionary<string, object> { { "true", true }, { "false", false } },
//                typeMap: new Dictionary<string, Type> {
//                    {"Int", typeof(int)},
//                    {"Bool", typeof(bool)},
//                    {"String", typeof(string)},
//                }
//            );
//        }

//    }
//}