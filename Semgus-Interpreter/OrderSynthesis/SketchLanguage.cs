using Semgus.Model.Smt;
using Semgus.Operational;
using System.Text;

namespace Semgus.OrderSynthesis {
    internal static class SketchLanguage {

        public enum PrimitiveType {
            Bit,
            Int,
        }

        public static SketchLanguage.PrimitiveType MapPrim(SmtSort sort) {
            if (sort.Name == SmtCommonIdentifiers.BoolSortId) return SketchLanguage.PrimitiveType.Bit;
            if (sort.Name == SmtCommonIdentifiers.IntSortId) return SketchLanguage.PrimitiveType.Int;
            throw new NotSupportedException();
        }

        public static void DoExpression(StringBuilder sb, Dictionary<string, NextVar> labelMap, ISmtLibExpression expression) {
            switch (expression) {
                case VariableEvalExpression varEval:
                    sb.Append(labelMap[varEval.Variable.Name].Name);
                    return;
                case LiteralExpression lit:
                    sb.Append(lit.BoxedValue.ToString()); // may not work in all cases
                    return;
                case FunctionCallExpression fcall:
                    if (SketchLanguage.TrySpecialHandling(sb, labelMap, fcall)) {
                        return;
                    } else if (SketchLanguage.TryAsOperator(fcall.Function.Name, out var opstring)) {
                        switch (fcall.Args.Count) {
                            case 0:
                                throw new Exception();
                            case 1:
                                sb.Append(opstring);
                                sb.Append('(');
                                DoExpression(sb, labelMap, fcall.Args[0]);
                                sb.Append(')');
                                break;
                            default:
                                sb.Append('(');
                                DoExpression(sb, labelMap, fcall.Args[0]);
                                for (int i = 1; i < fcall.Args.Count; i++) {
                                    sb.Append(' ');
                                    sb.Append(opstring);
                                    sb.Append(' ');
                                    DoExpression(sb, labelMap, fcall.Args[i]);
                                }
                                sb.Append(')');
                                break;
                        }
                    } else {
                        sb.Append(SketchLanguage.MapFname(fcall.Function.Name));

                        // Omit parens for unary functions, e.g. true / false in current impl
                        if (fcall.Args.Count > 0) {
                            sb.Append('(');
                            DoExpression(sb, labelMap, fcall.Args[0]);
                            for (int i = 1; i < fcall.Args.Count; i++) {
                                sb.Append(',');
                                sb.Append(' ');
                                DoExpression(sb, labelMap, fcall.Args[i]);
                            }
                            sb.Append(')');
                        }
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }
        public static string MapFname(string name) {
            return name;
        }
        public static bool TryAsOperator(string name, out object opstring) {
            switch (name) {
                case "!":
                case "not":
                    opstring = "!";
                    return true;
                case "=":
                    opstring = "==";
                    return true;
                case "+":
                case "-":
                case "*":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    opstring = name;
                    return true;
                case "and":
                    opstring = "&&";
                    return true;
                case "or":
                    opstring = "||";
                    return true;
            }
            opstring = default;
            return false;
        }

        public static bool TrySpecialHandling(StringBuilder sb, Dictionary<string, NextVar> labelMap, FunctionCallExpression fcall) {
            if (fcall.Function.Name == "ite") {
                if (fcall.Args.Count != 3) throw new Exception();
                sb.Append('(');
                DoExpression(sb, labelMap, fcall.Args[0]);
                sb.Append(" ? ");
                DoExpression(sb, labelMap, fcall.Args[1]);
                sb.Append(" : ");
                DoExpression(sb, labelMap, fcall.Args[2]);
                sb.Append(')');
                return true;
            }
            return false;
        }
    }
}