using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.SketchSyntax {
    using Op = SketchInfixOperator;

    internal class SketchFile {
        List<ISketchStatement> statements;
    }

    internal interface ILineReceiver {

        void IndentIn();
        void IndentOut();

        void Add(string line);
    }

    internal interface ISketchStatement {
        void WriteInto(ILineReceiver lineReceiver);
    }

    internal class SketchFunctionId {
        string name;
    }

    internal class SketchFunctionDef : ISketchStatement {
        public enum Kind {
            Regular,
            Harness,
            Generator
        }

        public SketchFunctionId name {get;}
        public Kind kind {get;}
        public SketchType return_type {get;}
        public IReadOnlyList<SketchArg> args { get; }

        public IReadOnlyList<ISketchStatement> body { get; }

        public SketchFunctionDef(SketchFunctionId name, Kind kind, SketchType return_type, IReadOnlyList<SketchArg> args, IReadOnlyList<ISketchStatement> body) {
            this.name = name;
            this.kind = kind;
            this.return_type = return_type;
            this.args = args;
            this.body = body;
        }

        public SketchFunctionDef(SketchFunctionId name, Kind kind, SketchType return_type, IReadOnlyList<SketchArg> args, params ISketchStatement[] body) {
            this.name = name;
            this.kind = kind;
            this.return_type = return_type;
            this.args = args;
            this.body = body;
        }

        static string GetPrefix(Kind kind) => kind switch {
            Kind.Regular => "",
            Kind.Harness => "harness ",
            Kind.Generator => "generator ",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"{GetPrefix(kind)}{return_type} ({string.Join(", ", args)}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }

    internal interface ISketchExpression { }

    internal class SketchVarDecl : ISketchStatement {
        SketchVarId id;
        SketchType type;
        ISketchExpression? def;
        public void WriteInto(ILineReceiver lineReceiver) {
            if (def is not null) {
                lineReceiver.Add($"{type} {id} = {def};");
            } else {
                lineReceiver.Add($"{type} {id};");
            }
        }
    }

    internal class SketchArg {
        SketchVarId id;
        SketchType type;

        public override string ToString() {
            return $"{type} {id}";
        }
    }

    internal class SketchVarId {
        string name;

        public override string ToString() => name;
    }

    internal class SketchVarAssign : ISketchStatement {
        SketchVarId var;
        ISketchExpression rhs;

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"{var} = {rhs};");
        }
    }

    internal class SketchHole : ISketchExpression {
        public string? Label { get; }

        public SketchHole(string? label = null) {
            Label = label;
        }

        public override string ToString() => Label is null ? "??" : $"?? /*{Label}*/";
    }

    internal class SketchLiteral : ISketchExpression {
        int value;
        public override string ToString() => value.ToString();
    }

    internal class SketchVarRead : ISketchExpression {
        SketchVarId var;
    }

    internal class SketchRepeat : ISketchStatement {
        ISketchExpression condition;
        List<ISketchStatement> body;

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"repeat({condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }

    internal class SketchAssert : ISketchStatement {
        ISketchExpression expr;

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"assert({expr});");
        }
    }

    internal class SketchReturn : ISketchStatement {
        ISketchExpression expr;
        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"return {expr};");
        }
    }

    enum SketchInfixOperator {
        Eq, Neq,
        Add, Sub, Mul,
        Or, And,
        Lt, Leq, Gt, Geq,
    }



    internal static class SketchEnumExt {
        public static string Str(this Op op) => op switch {
            Op.Eq => "==",
            Op.Neq => "!=",
            Op.Add => "+",
            Op.Sub => "-",
            Op.Mul => "*",
            Op.Or => "||",
            Op.And => "&&",
            Op.Lt => "<",
            Op.Leq => "<=",
            Op.Gt => ">",
            Op.Geq => ">=",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public static bool IsBin(this Op op) => op switch {
            Op.Eq or
            Op.Add or
            Op.Mul or
            Op.Or or
            Op.And => false,
            Op.Neq or
            Op.Sub or
            Op.Lt or
            Op.Leq or
            Op.Gt or
            Op.Geq => true,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public static SketchLanguage.PrimitiveType OutType(this Op op) => op switch {
            Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq or Op.Or or Op.And => SketchLanguage.PrimitiveType.Bit,
            Op.Add or Op.Sub or Op.Mul => SketchLanguage.PrimitiveType.Int,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public static bool ShouldParenthesize(this Op outer, Op inner, int index) => outer switch {
            Op.Sub => inner.OutType() == SketchLanguage.PrimitiveType.Bit || !(index == 0 || inner == Op.Mul),
            Op.Mul => inner != Op.Mul,
            Op.Add => inner.OutType() == SketchLanguage.PrimitiveType.Bit,
            Op.Eq or Op.Neq or Op.Lt or Op.Leq or Op.Gt or Op.Geq => inner.OutType() == SketchLanguage.PrimitiveType.Bit,
            Op.And => inner == Op.Or,
            Op.Or => inner == Op.And,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }



    internal class SketchPropAccessor : ISketchExpression {
        public ISketchExpression expr;
        public SketchVarId prop;

        public override string ToString() {
            return $"{expr}.{prop}";
        }
    }

    internal class SketchInfixOp : ISketchExpression {
        public Op Op { get; }
        public IReadOnlyList<ISketchExpression> Operands { get; }

        public override string ToString() {
            StringBuilder sb = new();

            for (int i = 0; i < Operands.Count; i++) {
                if (i > 0) {
                    sb.Append(' ');
                    sb.Append(Op.Str());
                    sb.Append(' ');
                }

                var e = Operands[i];

                if (e is SketchInfixOp inner && Op.ShouldParenthesize(inner.Op, i)) {
                    sb.Append('(');
                    sb.Append(inner);
                    sb.Append(')');
                } else {
                    sb.Append(e);
                }
            }
            return sb.ToString();
        }
    }

    internal class SketchFunctionCall : ISketchExpression {
        public SketchFunctionId Id { get; }
        public IReadOnlyList<ISketchExpression> Args { get; }

        public SketchFunctionCall(SketchFunctionId id, IReadOnlyList<ISketchExpression> args) {
            Id = id;
            Args = args;
        }

        public SketchFunctionCall(SketchFunctionId id, params ISketchExpression[] args) {
            Id = id;
            Args = args;
        }

        public override string ToString() {
            return $"{Id}({string.Join(", ", Args)}";
        }
    }

    internal class SketchIf : ISketchStatement {
        public ISketchExpression Condition { get; }
        public IReadOnlyList<ISketchStatement> Body { get; }

        public SketchIf(ISketchExpression condition, IReadOnlyList<ISketchStatement> body) {
            Condition = condition;
            Body = body;
        }

        public SketchIf(ISketchExpression condition, params ISketchStatement[] body) {
            Condition = condition;
            Body = body;
        }

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"if({Condition}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
        }
    }

    internal class SketchType {

    }
}