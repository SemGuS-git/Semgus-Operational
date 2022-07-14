using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Semgus.Util.Json;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.Subproblems;
using System.Diagnostics;
using Semgus.MiniParser;
using Semgus.OrderSynthesis.IntervalSemantics;

namespace Semgus.OrderSynthesis.OutputFormat {
    public class SnakeCaseNamingPolicy : JsonNamingPolicy {
        public override string ConvertName(string name) {

            if (name == null) throw new NullReferenceException();
            switch (name.Length) {
                case 0:
                    return "";
                case 1:
                    return char.ToLowerInvariant(name[0]).ToString();
                default:
                    var sb = new StringBuilder();
                    sb.Append(char.ToLowerInvariant(name[0]));
                    foreach (var c in name) {
                        if (char.IsUpper(c)) {
                            sb.Append('_');
                            sb.Append(char.ToLowerInvariant(c));
                        } else {
                            sb.Append(c);
                        }
                    }
                    return sb.ToString();
            }
        }
    }


    internal class Converters {
        public static Converters Instance { get; } = new();

        static class SexpHelper {
            public static string GetTypeName(Variable v) {
                if (v.TypeId == IntType.Id) {
                    return "int";
                }
                if (v.TypeId == BitType.Id) {
                    return "bool";
                }
                throw new NotSupportedException();
            }

            public static TypeLabel MapSketchType(Identifier id) {
                if (id == IntType.Id) {
                    return TypeLabel.Int;
                }
                if (id == BitType.Id) {
                    return TypeLabel.Bool;
                }
                throw new NotSupportedException();

            }


            public static string ToLatticeFnSexp(FunctionDefinition f) {
                if (f.Body.Count != 1 || f.Body[0] is not ReturnStatement ret) {
                    throw new NotImplementedException();
                }
                return ToLatticeFnSexp(ret.Expr, MapSketchType(f.Signature.ReturnTypeId));
            }
            public static List<string> ToLatticeFnSexpGroup(FunctionDefinition f, StructType returnType) {
                if (f.Body.Count != 1 || f.Body[0] is not ReturnStatement ret || ret.Expr is not StructNew stn) {
                    throw new NotSupportedException();
                }
                Debug.Assert(f.Signature.ReturnTypeId == returnType.Id);

                return stn.Args.Select((arg, i) => {
                    var v = (VariableRef)arg.Subject;
                    Debug.Assert($"v{i}" == v.TargetId.Name);
                    var etype = MapSketchType(returnType.Elements[i].TypeId);
                    return ToLatticeFnSexp(arg.Value, etype);
                }).ToList();
            }

            private static string ToLatticeFnSexp(IExpression expr, TypeLabel expected_type) {
                var sb = new StringBuilder();
                ToLatticeFnSexp(expr, expected_type, sb);
                return sb.ToString();
            }

            private static void ToLatticeFnSexp(IExpression expr, TypeLabel expected_type, StringBuilder sb) {
                switch (expr) {
                    case InfixOperation iop:
                        sb.Append('(');
                        var (a, b) = ToSmtOp(iop.Op);
                        sb.Append(a);
                        foreach (var arg in iop.Operands) {
                            sb.Append(' ');
                            ToLatticeFnSexp(arg, b, sb);
                        }
                        sb.Append(')');
                        break;
                    case UnaryOperation uop:
                        sb.Append('(');
                        var (c, d) = ToSmtOp(uop.Op);
                        sb.Append(c);
                        sb.Append(' ');
                        ToLatticeFnSexp(uop.Operand, d, sb);
                        sb.Append(')');
                        break;
                    case Ternary tern:
                        sb.Append("(ite ");
                        ToLatticeFnSexp(tern.Cond, TypeLabel.Bool, sb);
                        sb.Append(' ');
                        ToLatticeFnSexp(tern.ValIf, TypeLabel.Any, sb);
                        sb.Append(' ');
                        ToLatticeFnSexp(tern.ValElse, TypeLabel.Any, sb);
                        sb.Append(')');
                        break;
                    case Literal lit:
                        if (lit.Value > 1 || expected_type == TypeLabel.Int) {
                            sb.Append(lit);

                        } else if (expected_type == TypeLabel.Bool) {
                            Debug.Assert(lit.Value >= 0);
                            sb.Append(lit.Value > 0 ? "true" : "false");
                        } else {
                            throw new Exception("Ambiguous literal type");
                        }
                        break;
                    case PropertyAccess access:
                        Debug.Assert(access.Key.Name.StartsWith('v'));
                        var idx = int.Parse(access.Key.Name.Substring(1));
                        var which = ((VariableRef)access.Expr).TargetId.Name;
                        Debug.Assert(which == "a" || which == "b");
                        sb.Append(which);
                        sb.Append('.');
                        sb.Append(idx.ToString());
                        break;

                }
            }

            public static string StringifyBlockExpr(IBlockExpression be) {
                var sb = new StringBuilder();
                StringifyBlockExpr(be, sb);
                return sb.ToString();
            }

            private static void StringifyBlockExpr(IBlockExpression be, StringBuilder sb) {
                switch (be) {
                    case BlockExprLiteral lit:
                        sb.Append(lit.Value.ToString().ToLower()); // kludge
                        break;
                    case BlockExprRead read:
                        sb.Append('@');
                        sb.Append(read.Address.block_id);
                        sb.Append('.');
                        sb.Append(read.Address.slot);

                        break;
                    case BlockExprCall call:
                        sb.Append('(');
                        sb.Append(call.FunctionName);
                        foreach (var a in call.Args) {
                            sb.Append(' ');
                            StringifyBlockExpr(a, sb);
                        }
                        sb.Append(')');
                        break;
                }
            }

            static (string, TypeLabel) ToSmtOp(Op op) => op switch {
                Op.Eq => ("=", TypeLabel.Any),
                Op.Neq => ("distinct", TypeLabel.Any),
                Op.Plus => ("+", TypeLabel.Int),
                Op.Minus => ("-", TypeLabel.Int),
                Op.Times => ("*", TypeLabel.Int),
                Op.Or => ("or", TypeLabel.Bool),
                Op.And => ("and", TypeLabel.Bool),
                Op.Lt => ("<", TypeLabel.Int),
                Op.Leq => ("<=", TypeLabel.Int),
                Op.Gt => (">", TypeLabel.Int),
                Op.Geq => (">=", TypeLabel.Int),
                _ => throw new ArgumentOutOfRangeException(),
            };

            static (string, TypeLabel) ToSmtOp(UnaryOp op) => op switch {
                UnaryOp.Minus => ("-", TypeLabel.Int),
                UnaryOp.Not => ("not", TypeLabel.Bool),
                _ => throw new ArgumentOutOfRangeException(),
            };

            internal static IEnumerable<string> StringifyConstFunction(FunctionDefinition fdef, StructType type) {
                if (fdef.Body.Count != 1 ||
                    fdef.Body[0] is not ReturnStatement ret ||
                    ret.Expr is not StructNew sn ||
                    sn.Args.Count != type.Elements.Count
                ) throw new ArgumentException();

                for (var i = 0; i < sn.Args.Count; i++) {
                    Debug.Assert(sn.Args[i].Subject is VariableRef v && v.TargetId.Name == $"v{i}");
                    yield return ToLatticeFnSexp(sn.Args[i].Value, MapSketchType(type.Elements[i].TypeId));
                }
            }
        }


        public DelegateWriteOnlyConverter<LatticeDefs> ToBlockTypes { get; } = new(
            (writer, value, options) => {

                writer.WriteStartObject();

                // members
                writer.WriteStartArray("members");
                foreach (var a in value.type.Elements) {
                    writer.WriteStringValue(SexpHelper.GetTypeName(a));
                }
                writer.WriteEndArray();

                // cmp
                writer.WriteString("cmp", SexpHelper.ToLatticeFnSexp(value.compare));

                // top
                writer.WriteStartArray("top");
                SexpHelper.StringifyConstFunction(value.top, value.type).ToList().ForEach(writer.WriteStringValue);
                writer.WriteEndArray();

                // bot
                writer.WriteStartArray("bot");
                SexpHelper.StringifyConstFunction(value.bot, value.type).ToList().ForEach(writer.WriteStringValue);
                writer.WriteEndArray();


                // join_incomparable
                writer.WriteStartArray("join_incomparable");
                SexpHelper.ToLatticeFnSexpGroup(value.join_incomparable, value.type).ForEach(writer.WriteStringValue);
                writer.WriteEndArray();

                // meet_incomparable
                writer.WriteStartArray("meet_incomparable");
                SexpHelper.ToLatticeFnSexpGroup(value.meet_incomparable, value.type).ForEach(writer.WriteStringValue);
                writer.WriteEndArray();



                writer.WriteEndObject();
            }
        );

        public DelegateWriteOnlyConverter<BlockProduction> ToProdFmt { get; } = new(
            (writer, value, options) => {
                writer.WriteStartObject();
                writer.WriteNumber("term_type", value.TermTypeId);
                writer.WriteString("name", value.Name);
                writer.WriteStartArray("semantics");

                foreach (var sem in value.Semantics) {
                    Converters.Instance.ToSemFmt.Write(writer, sem, options);
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );
        public DelegateWriteOnlyConverter<BlockSemantics> ToSemFmt { get; } = new(
            (writer, value, options) => {
                writer.WriteStartObject();
                writer.WriteStartArray("block_types");
                foreach (var b in value.BlockTypes) writer.WriteNumberValue(b);
                writer.WriteEndArray();

                writer.WriteStartArray("steps");
                foreach (var step in value.Steps) {
                    switch (step) {
                        case BlockEval eval:
                            PutStep(writer, eval);
                            break;
                        case BlockAssert assert:
                            PutStep(writer, assert);
                            break;
                        case BlockAssign assign:
                            PutStep(writer, assign);
                            break;
                    }
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        );

        private static void PutStep(Utf8JsonWriter writer, BlockAssign assign) {
            writer.WriteStartObject();
            writer.WriteString("type", "set");

            writer.WriteNumber("target", assign.TargetBlockId);

            writer.WriteStartArray("required");
            foreach (var a in assign.RequiredBlocks) writer.WriteNumberValue(a);
            writer.WriteEndArray();

            writer.WriteStartArray("monotonicities");
            foreach (var a in assign.Monotonicities) writer.WriteStringValue(GetPrintName(a));
            writer.WriteEndArray();

            writer.WriteStartArray("expressions");
            foreach (var e in assign.Exprs) writer.WriteStringValue(SexpHelper.StringifyBlockExpr(e));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void PutStep(Utf8JsonWriter writer, BlockAssert assert) {
            writer.WriteStartObject();

            writer.WriteString("type", "assert");
            writer.WriteStartArray("required");
            foreach (var a in assert.RequiredBlockId) writer.WriteNumberValue(a);
            writer.WriteEndArray();
            writer.WriteStartArray("monotonicities");
            foreach (var a in assert.MonoLabels) writer.WriteStringValue(GetPrintName(a));
            writer.WriteEndArray();

            writer.WriteString("predicate", SexpHelper.StringifyBlockExpr(assert.Expression));
            writer.WriteEndObject();
        }

        private static void PutStep(Utf8JsonWriter writer, BlockEval eval) {
            writer.WriteStartObject();
            writer.WriteString("type", "eval");
            writer.WriteNumber("term", eval.TermIndex);
            writer.WriteNumber("input", eval.InputBlockId);
            writer.WriteNumber("output", eval.OutputBlockId);
            writer.WriteEndObject();
        }

        public DelegateWriteOnlyConverter<(FlatNt, LatticeDefs)> ToNtFmt { get; } = new(
            (writer, value, options) => {
                var (nt, lattice) = value;

                writer.WriteStartObject();
                writer.WriteString("name", nt.Name);
                writer.WriteNumber("term_type", nt.TermType);
                writer.WriteStartArray("productions");

                foreach (var prod in nt.Productions) {
                    writer.WriteStartObject();
                    writer.WriteNumber("prod_idx", prod.prod_idx);
                    writer.WriteStartArray("child_nts");
                    foreach (var ch in prod.child_nt_ids) writer.WriteNumberValue(ch);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteStartArray("bounds_universal");

                writer.WriteStartArray();
                SexpHelper.StringifyConstFunction(lattice.bot, lattice.type).ToList().ForEach(writer.WriteStringValue);
                writer.WriteEndArray();

                writer.WriteStartArray();
                SexpHelper.StringifyConstFunction(lattice.top, lattice.type).ToList().ForEach(writer.WriteStringValue);
                writer.WriteEndArray();

                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );

        private static string GetPrintName(Monotonicity a) => a switch {
            Monotonicity.None => "none",
            Monotonicity.Increasing => "increasing",
            Monotonicity.Decreasing => "decreasing",
            Monotonicity.Constant => "constant",
        };

        public DelegateWriteOnlyConverter<(InitialStuff, IReadOnlyList<LatticeDefs>)> ToOutputDocument { get; } = new DelegateWriteOnlyConverter<(InitialStuff, IReadOnlyList<LatticeDefs>)>(

            (writer, value, options) => {
                var (lang, lattices) = value;

                writer.WriteStartObject();

                writer.WriteStartArray("block_types");
                foreach (var lattice in lattices) Converters.Instance.ToBlockTypes.Write(writer, lattice, options);
                writer.WriteEndArray();

                writer.WriteStartArray("productions");
                foreach (var prod in lang.Productions) Converters.Instance.ToProdFmt.Write(writer, prod, options);
                writer.WriteEndArray();

                writer.WriteNumber("start_symbol", 0);

                writer.WriteStartArray("nonterminals");
                foreach (var nt in lang.Nonterminals) Converters.Instance.ToNtFmt.Write(writer, (nt, lattices[lang.TypeHelper.GetOutputBlockType(nt.TermType).Id]), options);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );

        public static string Serialize(InitialStuff a, IReadOnlyList<LatticeDefs> b) {
            var opt = new JsonSerializerOptions() {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(), // write SomeProperty as "some_property"
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // don't escape unicode symbols
            };
            opt.Converters.Add(Instance.ToOutputDocument);
            return JsonSerializer.Serialize((a, b), opt);
        }

    }
}
