//using System;
//using System.Collections.Generic;
//using Semgus.Syntax;

//namespace Semgus.Interpreter {
//    /// <summary>
//    /// Types and functions needed for interpretation.
//    /// This class forms the connection between the interpreter and SMT-LIB2.
//    /// </summary>
//    public class Theory {
//        public string Identifier { get; }
//        private readonly IReadOnlyDictionary<string, SmtLibFunction> _functions;
//        private readonly IReadOnlyDictionary<string, object> _constants;
//        private readonly IReadOnlyDictionary<string, Type> _typeMap;

//        public Theory(string identifier, IReadOnlyDictionary<string, SmtLibFunction> functions, IReadOnlyDictionary<string,object> constants, IReadOnlyDictionary<string, Type> typeMap) {
//            Identifier = identifier;
//            _functions = functions;
//            _constants = constants;
//            _typeMap = typeMap;
//        }

//        public Type GetType(SemgusType type) => _typeMap[type.Name];
//        public SmtLibFunction GetFunction(LibraryFunction libraryFunction) => _functions[libraryFunction.Name];

//        public object GetConstant(string identifier) => _constants[identifier];

//        public static bool TryGetTheory(string key, out Theory value) {
//            switch(key) {
//                case "lia":
//                    value = BasicLibrary.Instance;
//                    return true;
//                case "string":
//                    value = StringLibrary.Instance;
//                    return true;
//                case "regex_bmat":
//                    value = RegexBoolMatLibrary.Instance;
//                    return true;
//                default:
//                    value = default;
//                    return false;
//            }
//        }
//    }
//}