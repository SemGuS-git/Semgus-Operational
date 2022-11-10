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
using Semgus.Operational;
using Semgus.Constraints;
using Semgus.Util;

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

    internal class SpecConverters {
        public static SpecConverters Instance { get; } = new();

        internal static string Serialize(GrammarIndexing g_idx, InterpretationGrammar grammar, InductiveConstraint constraint) {
            var opt = new JsonSerializerOptions() {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(), // write SomeProperty as "some_property"
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // don't escape unicode symbols
            };
            opt.Converters.Add(Instance.ToOutputDoc);
            return JsonSerializer.Serialize((g_idx, grammar, constraint), opt);
        }

        private static string[] FindIOLabels(InterpretationGrammar grammar, NtSymbol nt) {
            if (TryFindIOLabels(grammar, nt, new(), out var labels)) {
                return labels;
            } else {
                throw new InvalidDataException("Error: start symbol cannot produce a concrete term");
            }
        }
        private static bool TryFindIOLabels(InterpretationGrammar grammar, NtSymbol nt, HashSet<NtSymbol> seen, out string[] labels) {
            var p = grammar.Productions[nt];
            if (p.Count > 0) {
                var representative = p[0].Production;
                labels = new string[representative.InputVariables.Count + representative.OutputVariables.Count];
                foreach (var a in representative.InputVariables) labels[a.Index] = "in";
                foreach (var a in representative.OutputVariables) labels[a.Index] = "out";
                return true;
            }

            // If this NT has no operator productions (e.g., A ::= B | C), recurse
            seen.Add(nt);
            var pt = grammar.PassthroughProductions[nt];


            foreach (var g in pt) {
                // The hashset prevents issues due to cycles in the grammar
                if (!seen.Contains(g) && TryFindIOLabels(grammar, g, seen, out labels)) {
                    return true;
                }
            }


            labels = default;
            return false;
        }

        public DelegateWriteOnlyConverter<(
            GrammarIndexing g_idx,
            InterpretationGrammar grammar,
            InductiveConstraint constraint
        )> ToOutputDoc { get; } = new(
            (writer, value, options) => {
                var (g_idx, grammar, constraint) = value;

                var labels = FindIOLabels(grammar, g_idx.OrderedNonterminals[0]);

                writer.WriteStartObject();
                writer.WriteStartArray("arg_labels");
                foreach (var a in labels) writer.WriteStringValue(a);
                writer.WriteEndArray();

                writer.WriteStartArray("examples");
                foreach (var e in constraint.Examples) {
                    writer.WriteStringValue(string.Join(' ', e.Values).ToLower());
                }
                writer.WriteEndArray();

                writer.WriteNumber("start_symbol", 0);

                writer.WriteStartArray("nonterminals");
                for (int i = 0; i < g_idx.OrderedNonterminals.Count; i++) {
                    var nt = g_idx.OrderedNonterminals[i];
                    SpecConverters.Instance.ToNtFmt.Write(writer, (nt, grammar.Productions[nt], grammar.PassthroughProductions[nt], g_idx), options);


                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        );

        public DelegateWriteOnlyConverter<(NtSymbol nt, IReadOnlyList<NonterminalProduction> prods, IReadOnlyList<NtSymbol> pt_prods, GrammarIndexing g_idx)> ToNtFmt { get; } = new(
            (writer, value, options) => {
                var (nt, prods, pt_prods, g_idx) = value;

                writer.WriteStartObject();
                writer.WriteString("name", nt.Name);
                writer.WriteNumber("term_type", g_idx.GetTermTypeId(nt));
                writer.WriteStartArray("productions");

                foreach (var prod in prods) {
                    writer.WriteStartObject();
                    writer.WriteNumber("prod_id", prod.Production.SequenceNumber);
                    writer.WriteStartArray("child_nts");
                    foreach (var ch in prod.ChildNonterminals) writer.WriteNumberValue(g_idx.NtIds[ch]);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteStartArray("passthrough_productions");

                foreach (var other_nt in pt_prods) {
                    writer.WriteStartObject();
                    writer.WriteNumber("nt_id", g_idx.NtIds[other_nt]);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );

    }

    internal class ConcConverters {
        public static ConcConverters Instance { get; } = new();


        public DelegateWriteOnlyConverter<InterpretationLibrary> ToOutputDoc { get; } = new(
            (writer, value, options) => {
                writer.WriteStartObject();



                writer.WriteStartArray("productions");
                foreach (var prod in value.Productions) {
                    writer.WriteStartObject();
                    writer.WriteString("name", prod.SyntaxConstructor.Operator.AsString());
                    writer.WriteNumber("id", prod.SequenceNumber);
                    writer.WriteStartArray("semantics");

                    foreach (var sem in prod.Semantics) {
                        writer.WriteStartObject();
                        writer.WriteStartArray("steps");

                        foreach (var step in sem.Steps) {
                            writer.WriteStringValue(ToSexp(step));
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );

        //public DelegateWriteOnlyConverter<InterpretationGrammar> Tox { get; } = new(
        //    (writer, value, options) => {
        //        writer.WriteStartObject();



        //        writer.WriteStartArray("productions");
        //        foreach (var kvp in value.Productions) {
        //            writer.WriteStartObject();
        //            writer.WriteString("name", prod.ToString());
        //            writer.WriteStartArray("semantics");

        //            foreach (var sem in prod.Semantics) {
        //                writer.WriteStartObject();
        //                writer.WriteStartArray("steps");

        //                foreach (var step in sem.Steps) {
        //                    writer.WriteStringValue(ToSexp(step));
        //                }

        //                writer.WriteEndArray();
        //                writer.WriteEndObject();
        //            }

        //            writer.WriteEndArray();
        //            writer.WriteEndObject();
        //        }

        //        writer.WriteEndArray();

        //        writer.WriteEndObject();
        //    }
        //);

        internal static string Serialize(InterpretationLibrary library) {
            var opt = new JsonSerializerOptions() {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(), // write SomeProperty as "some_property"
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // don't escape unicode symbols
            };
            opt.Converters.Add(Instance.ToOutputDoc);
            return JsonSerializer.Serialize(library, opt);
        }

        private record Wrap(string head, List<object> tail) {
            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append('(');
                sb.Append(head);
                foreach (var a in tail) {
                    sb.Append(' ');
                    if (a is ISmtLibExpression expr) {
                        Stringify(expr, sb);
                    } else {
                        sb.Append(a.ToString());
                    }
                }
                sb.Append(')');
                return sb.ToString();
            }
        }

        private static string Stringify(ISmtLibExpression expr) {
            var sb = new StringBuilder();
            Stringify(expr, sb);
            return sb.ToString();
        }

        private static void Stringify(ISmtLibExpression expr, StringBuilder sb) {
            switch (expr) {
                case VariableEvalExpression vee:
                    sb.Append(Encode(vee.Variable));
                    return;
                case LiteralExpression lit:
                    sb.Append(lit.BoxedValue.ToString());
                    return;
                case FunctionCallExpression fce:
                    if (fce.Args.Count == 0) {
                        sb.Append(fce.Function.Name);
                        return;
                    } else {
                        sb.Append('(');
                        sb.Append(fce.Function.Name);
                        foreach (var a in fce.Args) {
                            sb.Append(' ');
                            Stringify(a, sb);
                        }
                        sb.Append(')');
                        return;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static string Encode(VariableInfo vee) {
            return $"{vee.Name}@{vee.Index}";
        }

        private static string ToSexp(IInterpretationStep step) {
            switch (step) {
                case AssignmentFromLocalFormula assign:
                    return new Wrap("SET", new() { Encode(assign.ResultVar), assign.Expression }).ToString();
                case TermEvaluation eval:
                    return new Wrap("EVAL",
                        eval.Args.Select(a =>
                            (object)new Wrap(a.isOutput ? ":out" : ":in", new() { Encode(a.info) })
                        ).Prepend(eval.Term.Index).ToList()
                    ).ToString();
                case ConditionalAssertion assertion:
                    return new Wrap("ASSERT", new() { assertion.Expression }).ToString();
                default:
                    throw new NotSupportedException();
            }
        }
    }
    internal class Converters {
        public static Converters Instance { get; } = new();

        private ITheoryImplementation SmtTheory { get; } = MakeTheory();

        static ITheoryImplementation MakeTheory() {
            var smt = new Model.Smt.SmtContext();
            var sortHelper = new SortHelper(smt);
            return new UnionTheoryImpl(new ITheoryImplementation[] {
                new SmtCoreTheoryImpl(sortHelper),
                new SmtIntsTheoryImpl(sortHelper),
            });
        }

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

            static (string symbol, TypeLabel arg_type) ToSmtOp(Op op) => op switch {
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
                    yield return FlattenToLiteral(sn.Args[i].Value, MapSketchType(type.Elements[i].TypeId));
                }
            }

            private static string FlattenToLiteral(IExpression expr, TypeLabel typeLabel) {
                return EvalConstExpr(expr, typeLabel).ToString().ToLower();
            }

            private static object EvalConstExpr(IExpression expr, TypeLabel typeLabel) {
                switch (expr) {
                    case Literal lit:
                        return typeLabel switch {
                            TypeLabel.Int or TypeLabel.Any => lit.Value,
                            TypeLabel.Bool => lit.Value == 0 ? false : true,
                            _ => throw new NotSupportedException(),
                        };
                    case UnaryOperation unOp: {
                            var (_, arg_type) = ToSmtOp(unOp.Op);
                            var value = EvalConstExpr(unOp.Operand, arg_type);
                            return unOp.Op switch {
                                UnaryOp.Not => !((bool)value),
                                UnaryOp.Minus => -((int)value),
                            };
                        }
                    case InfixOperation infixOp: {
                            var (_, arg_type) = ToSmtOp(infixOp.Op);
                            var values = infixOp.Operands.Select(arg => EvalConstExpr(arg, arg_type)).ToList();
                            return infixOp.Op switch {
                                Op.Eq => values.Distinct().Count() == 1,
                                Op.Neq => values.Distinct().Count() > 1,
                                Op.Plus => values.Cast<int>().Sum(),
                                Op.Minus => values.Count switch {
                                    1 => -((int)values[0]),
                                    2 => ((int)values[1]) - ((int)values[0]),
                                    _ => throw new ArgumentException(),
                                },
                                Op.Or => values.Cast<bool>().Any(),
                                Op.And => values.Cast<bool>().All(b => b),
                                _ => throw new NotSupportedException(),
                            };
                        }
                    case Ternary tern: {
                            return EvalConstExpr(((bool)EvalConstExpr(tern.Cond, TypeLabel.Bool)) ? tern.ValIf : tern.ValElse, TypeLabel.Any);
                        }
                    default: throw new NotSupportedException();
                };
            }
        }

        public DelegateWriteOnlyConverter<IRichTupleDescriptor> ToBlockTypes { get; } = new(
            (writer, value, options) => {

                writer.WriteStartObject();

                // members
                writer.WriteStartArray("members");
                foreach (var a in value.type.Elements) {
                    writer.WriteStringValue(SexpHelper.GetTypeName(a));
                }
                writer.WriteEndArray();

                if (value is LatticeDefs lattice) {
                    // cmp
                    writer.WriteString("cmp", SexpHelper.ToLatticeFnSexp(lattice.compare));

                    // top
                    writer.WriteStartArray("top");
                    SexpHelper.StringifyConstFunction(lattice.top, lattice.type).ToList().ForEach(writer.WriteStringValue);
                    writer.WriteEndArray();

                    // bot
                    writer.WriteStartArray("bot");
                    SexpHelper.StringifyConstFunction(lattice.bot, lattice.type).ToList().ForEach(writer.WriteStringValue);
                    writer.WriteEndArray();


                    // join_incomparable
                    writer.WriteStartArray("join_incomparable");
                    SexpHelper.ToLatticeFnSexpGroup(lattice.join_incomparable, lattice.type).ForEach(writer.WriteStringValue);
                    writer.WriteEndArray();

                    // meet_incomparable
                    writer.WriteStartArray("meet_incomparable");
                    SexpHelper.ToLatticeFnSexpGroup(lattice.meet_incomparable, lattice.type).ForEach(writer.WriteStringValue);
                    writer.WriteEndArray();
                }


                writer.WriteEndObject();
            }
        );

        public DelegateWriteOnlyConverter<BlockProduction> ToProdFmt { get; } = new(
            (writer, value, options) => {
                writer.WriteStartObject();
                writer.WriteNumber("term_type", value.TermTypeId);
                writer.WriteString("name", value.Name);
                writer.WriteNumber("id", value.SequenceNumber);
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

        public DelegateWriteOnlyConverter<(TupleLibrary, IReadOnlyList<IRichTupleDescriptor>)> ToOutputDocument { get; } = new DelegateWriteOnlyConverter<(TupleLibrary, IReadOnlyList<IRichTupleDescriptor>)>(

            (writer, value, options) => {
                var (lang, lattices) = value;

                writer.WriteStartObject();

                writer.WriteStartArray("block_types");
                foreach (var lattice in lattices) Converters.Instance.ToBlockTypes.Write(writer, lattice, options);
                writer.WriteEndArray();

                writer.WriteStartArray("productions");
                foreach (var prod in lang.Productions) Converters.Instance.ToProdFmt.Write(writer, prod, options);
                writer.WriteEndArray();

                writer.WriteStartArray("nonterminals");
                foreach (var nt in lang.Nonterminals) {
                    int tt_id = lang.TypeHelper.GetOutputBlockType(nt.TermType).Id;
                    Converters.Instance.ToNtFmt.Write(writer, (nt, (LatticeDefs)lattices[tt_id]), options);
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        );

        public static string Serialize(TupleLibrary a, IReadOnlyList<IRichTupleDescriptor> b) {
            var opt = new JsonSerializerOptions() {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(), // write SomeProperty as "some_property"
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // don't escape unicode symbols
            };
            opt.Converters.Add(Instance.ToOutputDocument);
            return JsonSerializer.Serialize((a, b), opt);
        }

    }
}
