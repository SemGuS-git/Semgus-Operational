//using Semgus.Syntax;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Semgus.Interpreter {
//    /// <summary>
//    /// Interpreted function sourced from a theory.
//    /// </summary>
//    public class SmtLibFunction {
//        public class TypeSignature {
//            public Type OutputType {get;}
//            private readonly Type[] _inputTypes;
//            private readonly Type _starInputType;

//            public TypeSignature(Type outputType, Type[] inputTypes, Type starInputType = null) {
//                this.OutputType = outputType;
//                this._inputTypes = inputTypes;
//                this._starInputType = starInputType;
//            }
            
//            public bool TypeCheck(IReadOnlyList<ISmtLibExpression> args) {
//                if(_starInputType is null) {
//                    if(args.Count != _inputTypes.Length) return false;
//                } else {
//                    if(args.Count < _inputTypes.Length) return false;
//                }
                
//                int k = 0;
                
//                for(; k < _inputTypes.Length; k++) {
//                    if(!args[k].ResultType.IsAssignableTo(_inputTypes[k])) return false;
//                }
//                for(; k < args.Count; k++) {
//                    if(!args[k].ResultType.IsAssignableTo(_starInputType)) return false;
//                }
                
//                return true;
//            }

//            public string PrettyPrintInputs() {
//                var sb = new StringBuilder();
//                sb.Append('[');
//                sb.Append(string.Join(", ", _inputTypes.Select(t => t.Name)));

//                if (!(_starInputType is null)) {
//                    if (_inputTypes.Length > 0) {
//                        sb.Append(", ");
//                    }
//                    sb.Append(_starInputType.Name);
//                    sb.Append('*');
//                }
//                sb.Append(']');
//                return sb.ToString();
//            }

//            /// <summary>
//            /// Parse a type signature of the form "string int* -> int".
//            /// </summary>
//            /// <param name="str"></param>
//            /// <returns></returns>
//            public static TypeSignature Parse(string str) {
//                var parts = str.Split("->");
//                if (parts.Length != 2) throw new ArgumentException();

//                var inputs = parts[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);

//                var reqArgCount = inputs.Length;
//                Type starArgType = null;

//                if (reqArgCount > 0) {
//                    var lastInput = inputs[inputs.Length - 1];
//                    if (lastInput.EndsWith('*')) {
//                        starArgType = MapType(lastInput.Substring(0, lastInput.Length - 1));
//                        reqArgCount -= 1;
//                    }
//                }

//                var inputTypes = new Type[reqArgCount];

//                for(int i = 0; i < reqArgCount; i++) {
//                    inputTypes[i] = MapType(inputs[i]);
//                }

//                var outputs = parts[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
//                if (outputs.Length != 1) throw new ArgumentException("Functions may only have single output");
//                var outputType = MapType(outputs[0]);

//                return new TypeSignature(outputType, inputTypes, starArgType);
//            }

//            private static Type MapType(string s) {
//                if (TryMapShorthandType(s, out var t)) return t;
//                return Type.GetType(s) ?? throw new KeyNotFoundException();
//            }

//            private static bool TryMapShorthandType(string s, out Type T) {
//                switch (s) {
//                    case "string":
//                    case "String":
//                        T = typeof(string);
//                        return true;
//                    case "int":
//                    case "Int":
//                        T = typeof(int);
//                        return true;
//                    case "bool":
//                    case "Bool":
//                        T = typeof(bool);
//                        return true;
//                    case "object":
//                    case "Object":
//                        T = typeof(object);
//                        return true;
//                    case "BoolMat":
//                        T = typeof(BoolMat);
//                        return true;
//                }
//                T = default;
//                return true;
//            }
//        }

//        public delegate object Evaluator(object[] args);

//        public string Name { get; }
//        public TypeSignature Signature { get; }
//        public Evaluator Evaluate { get; }

//        public SmtLibFunction(string name, TypeSignature signature, Evaluator evaluate) {
//            Name = name;
//            Signature = signature;
//            Evaluate = evaluate;
//        }

//        public SmtLibFunction(string name, string signatureStr, Evaluator evaluate) {
//            Name = name;
//            Signature = TypeSignature.Parse(signatureStr);
//            Evaluate = evaluate;
//        }

//        public void AssertTypeCheck(IReadOnlyList<ISmtLibExpression> args) {
//            if (!Signature.TypeCheck(args)) throw new Exception($"Function {Name} expected types {Signature.PrettyPrintInputs()}, got [{string.Join(", ", args.Select(a => a.ResultType.Name))}]");
//        }

//    }
//}