using Semgus.MiniParser;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.Subproblems;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.AbstractInterpretation {

    internal class MuxTupleType {

        public int Size { get; }
        public IReadOnlyList<Type> ElementTypes { get; }

        private readonly IExpression _compare;
        private readonly StructNew _join_incomparable;
        private readonly StructNew _meet_incomparable;
        private readonly StructNew _top;
        private readonly StructNew _bot;

        private readonly MicroInterpreter _interpreter;

        public MuxTuple Top { get; private set; } // Lattice top element
        public MuxTuple Bot { get; private set; } // Lattice bot element

        public MuxTuple Instantiate(object[] values) {
            Debug.Assert(values.Length == Size);
            return new(this, values);
        }

        private MuxTupleType(LatticeDefs defs) {
            (var type, var compare, var top, var bot, var join_incomparable, var meet_incomparable) = defs;
            Size = type.Elements.Count;

            ElementTypes = type.Elements.Select(e => DotNetTypeOf(e.TypeId)).ToList();

            _interpreter = new(type.Elements);
            _compare = TakeSingleExpr(compare);
            _top = (StructNew)TakeSingleExpr(top);
            _bot = (StructNew)TakeSingleExpr(bot);
            _join_incomparable = (StructNew)TakeSingleExpr(join_incomparable);
            _meet_incomparable = (StructNew)TakeSingleExpr(meet_incomparable);
        }

        static Type DotNetTypeOf(Identifier id) {
            if (id == BitType.Id) return typeof(bool);
            if (id == IntType.Id) return SmtIntsTheoryImpl.IntegerType;
            throw new NotSupportedException();
        }

        private void InitExtrema() {
            Top = _interpreter.EvalStruct(this, _top, default, default);
            Bot = _interpreter.EvalStruct(this, _bot, default, default);
        }

        public static MuxTupleType Make(LatticeDefs defs) {
            MuxTupleType a = new(defs);
            a.InitExtrema();
            return a;
        }


        private static IExpression TakeSingleExpr(FunctionDefinition def) {
            Debug.Assert(def.Body.Count == 1);
            return ((ReturnStatement)def.Body[0]).Expr;
        }


        public MuxTuple Join(MuxTuple a, MuxTuple b) {
            if (Compare(a, b)) return b;
            if (Compare(b, a)) return a;
            return _interpreter.EvalStruct(this, _join_incomparable, a, b);
        }

        public MuxTuple Meet(MuxTuple a, MuxTuple b) {
            if (Compare(a, b)) return a;
            if (Compare(b, a)) return b;
            return _interpreter.EvalStruct(this, _meet_incomparable, a, b);
        }

        public bool StrictCompare(MuxTuple a, MuxTuple b) => Compare(a, b) && !a.Values.SequenceEqual(b.Values);


        public bool Compare(MuxTuple a, MuxTuple b) => Convert.ToBoolean(_interpreter.Eval(_compare, a, b));


        class MicroInterpreter {
            private readonly int _size;
            private readonly Dictionary<Identifier, int> _indexMap;

            public MicroInterpreter(IReadOnlyList<Variable> myTypeIds) {
                _size = myTypeIds.Count;
                _indexMap = new();
                for (int i = 0; i < myTypeIds.Count; i++) _indexMap.Add(myTypeIds[i].Id, i);
            }


            public dynamic Eval(IExpression expr, MuxTuple a, MuxTuple b) => expr switch {
                Literal lit => Convert.ChangeType(lit.Value,SmtIntsTheoryImpl.IntegerType), // This also consumes bit literals; we'll cast those later as appropriate
                PropertyAccess pa => Extract(pa, a, b),
                Ternary tern => Convert.ToBoolean(Eval(tern.Cond, a, b)) ? Eval(tern.ValIf, a, b) : Eval(tern.ValElse, a, b),
                InfixOperation infix => DoInfix(infix, a, b),
                UnaryOperation unary => unary.Op switch {
                    UnaryOp.Not => ! Convert.ToBoolean(Eval(unary.Operand, a, b)),
                    UnaryOp.Minus => -Eval(unary.Operand, a, b),
                    _ => throw new ArgumentOutOfRangeException(),
                },
                _ => throw new Exception(),
            };

            private dynamic DoInfix(InfixOperation infix, MuxTuple a, MuxTuple b) {
                dynamic first = Eval(infix.Operands[0], a, b);
                for (int i = 1; i < infix.Operands.Count; i++) {
                    first = infix.Op switch {
                        Op.Eq => first == Eval(infix.Operands[i], a, b),
                        Op.Neq => first != Eval(infix.Operands[i], a, b),
                        Op.Plus => first + Eval(infix.Operands[i], a, b),
                        Op.Minus => first - Eval(infix.Operands[i], a, b),
                        Op.Times => first * Eval(infix.Operands[i], a, b),
                        Op.Or => Convert.ToBoolean(first) || Convert.ToBoolean(Eval(infix.Operands[i], a, b)),
                        Op.And => Convert.ToBoolean(first) && Convert.ToBoolean(Eval(infix.Operands[i], a, b)),
                        Op.Lt => first < Eval(infix.Operands[i], a, b),
                        Op.Leq => first <= Eval(infix.Operands[i], a, b),
                        Op.Gt => first > Eval(infix.Operands[i], a, b),
                        Op.Geq => first >= Eval(infix.Operands[i], a, b),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
                return first;
            }

            private dynamic Extract(PropertyAccess pa, MuxTuple a, MuxTuple b) => ((VariableRef)pa.Expr).TargetId.Name switch {
                "a" => a[_indexMap[pa.Key]],
                "b" => b[_indexMap[pa.Key]],
                _ => throw new Exception(),
            };

            public MuxTuple EvalStruct(MuxTupleType outType, StructNew expr, MuxTuple a, MuxTuple b) {
                dynamic[] vals = new dynamic[outType.Size];

                for (int i = 0; i < expr.Args.Count; i++) {
                    Assignment? arg = expr.Args[i];
                    var value = Convert.ChangeType(Eval(arg.Value, a, b), outType.ElementTypes[i]);
                    vals[_indexMap[((VariableRef)arg.Subject).TargetId]] = value;
                }
                return new(outType, vals);
            }
        }

    }



    //internal class Agglom {
    //    SemgusTermType TermType { get; }
    //    SynthComparisonFunction CompareIn { get; }
    //    SynthComparisonFunction CompareOut { get; }
    //}

    //internal class Interval {

    //}
    //internal class SemRelArgTupleType {

    //

    //internal class SynthComparisonFunction {
    //    public bool Leq() {

    //    }
    //}
}
