using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace Semgus.OrderSynthesis.SketchSyntax.Sugar {
    internal static class X {
        public static Literal L0 { get; } = new Literal(0);
        public static Literal L1 { get; } = new Literal(1);
        public static Literal L2 { get; } = new Literal(2);
        public static Literal L3 { get; } = new Literal(3);
        public static Literal L4 { get; } = new Literal(4);
        public static Literal L5 { get; } = new Literal(5);
        public static Literal L6 { get; } = new Literal(6);

        public static InfixOperation Bi(IExpression lhs, Op op, IExpression rhs) => new InfixOperation(op, lhs, rhs);

    }

    internal static class ExpressionExtensions {
        public static IfStatement IfEq(this IExpression cond_lhs, int cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, new Literal(cond_rhs)), body);
        public static IfStatement IfEq(this IExpression cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, cond_rhs), body);

        public static ElseIfStatement ElseIfEq(this IExpression cond_lhs, int cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, new Literal(cond_rhs)), body);
        public static ElseIfStatement ElseIfEq(this IExpression cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, cond_rhs), body);

        public static InfixOperation Implies(this IExpression lhs, IExpression rhs) => new(Op.Or, new FunctionEval(LibFunctions.Not, lhs), rhs);
        public static InfixOperation ImpliedBy(this IExpression lhs, IExpression rhs) => new(Op.Or, lhs, new FunctionEval(LibFunctions.Not, rhs));

        public static FunctionEval Call(this FunctionDefinition fn, params IExpression[] args) => new(fn.Id, args);
        public static FunctionEval Call(this FunctionDefinition fn, IReadOnlyList<IExpression> args) => new(fn.Id, args);
        public static FunctionEval Call(this FunctionId fn_id, params IExpression[] args) => new(fn_id, args);
        public static FunctionEval Call(this FunctionId fn_id, IReadOnlyList<IExpression> args) => new(fn_id, args);

        public static FunctionEval Compare(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(st.CompareId, lhs, rhs);
        public static FunctionEval NotCompare(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.CompareId, lhs, rhs));
        public static FunctionEval Equal(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(st.EqId, lhs, rhs);
        public static FunctionEval NotEqual(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.EqId, lhs, rhs));

        public static InfixOperation Of(this Op op, params IExpression[] operands) => new InfixOperation(op, operands);

        public static Assignment Set(this ISettable subject, IExpression value) => new Assignment(subject, value);
        public static VarDeclare Declare(this VarId var_id, IExpression? value = null) => new VarDeclare(var_id, value);

        public static PropertyAccess Prop(this VarId id, VarId prop) => new PropertyAccess(id, prop);
    }

}
