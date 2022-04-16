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
        public static InfixOperation Bi(Variable lhs, Op op, Variable rhs) => new InfixOperation(op, lhs.Ref(), rhs.Ref());

    }

    internal static class ExpressionExtensions {
        public static IfStatement IfEq(this IExpression cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, cond_rhs), body);
        public static IfStatement IfEq(this Variable cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs.Ref(), cond_rhs), body);
        public static ElseIfStatement ElseIfEq(this IExpression cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, cond_rhs), body);

        public static ElseIfStatement ElseIfEq(this Variable cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs.Ref(), cond_rhs), body);





        public static InfixOperation Implies(this IExpression lhs, IExpression rhs) => new(Op.Or, new FunctionEval(LibFunctions.Not, lhs), rhs);
        public static InfixOperation ImpliedBy(this IExpression lhs, IExpression rhs) => new(Op.Or, lhs, new FunctionEval(LibFunctions.Not, rhs));
        public static InfixOperation Implies(this Variable lhs, Variable rhs) => new(Op.Or, new FunctionEval(LibFunctions.Not, lhs.Ref()), rhs.Ref());
        public static InfixOperation ImpliedBy(this Variable lhs, Variable rhs) => new(Op.Or, lhs.Ref(), new FunctionEval(LibFunctions.Not, rhs.Ref()));

        public static FunctionEval Call(this FunctionDefinition fn, params IExpression[] args) => new(fn.Id, args);
        public static FunctionEval Call(this FunctionDefinition fn, IReadOnlyList<IExpression> args) => new(fn.Id, args);
        public static FunctionEval Call(this Identifier fn_id, params IExpression[] args) => new(fn_id, args);
        public static FunctionEval Call(this Identifier fn_id, IReadOnlyList<IExpression> args) => new(fn_id, args);

        public static FunctionEval Compare(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(st.CompareId, lhs, rhs);
        public static FunctionEval Compare(this StructType st, Variable lhs, Variable rhs) => new FunctionEval(st.CompareId, lhs.Ref(), rhs.Ref());
        public static FunctionEval NotCompare(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.CompareId, lhs, rhs));
        public static FunctionEval NotCompare(this StructType st, Variable lhs, Variable rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.CompareId, lhs.Ref(), rhs.Ref()));
        public static FunctionEval Equal(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(st.EqId, lhs, rhs);
        public static FunctionEval Equal(this StructType st, Variable lhs, Variable rhs) => new FunctionEval(st.EqId, lhs.Ref(), rhs.Ref());
        public static FunctionEval NotEqual(this StructType st, IExpression lhs, IExpression rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.EqId, lhs, rhs));
        public static FunctionEval NotEqual(this StructType st, Variable lhs, Variable rhs) => new FunctionEval(LibFunctions.Not, new FunctionEval(st.EqId, lhs.Ref(), rhs.Ref()));

        public static InfixOperation Of(this Op op, params IExpression[] operands) => new InfixOperation(op, operands);

        public static Assignment Assign(this Variable subject, IExpression value) => new Assignment(subject.Ref(), value);
        public static Assignment Assign(this ISettable subject, IExpression value) => new Assignment(subject, value);
        public static VariableDeclaration Declare(this Variable var_id, IExpression? value = null) => new VariableDeclaration(var_id, value);

        public static PropertyAccess Get(this Variable v, Identifier key) => new PropertyAccess(v.Ref(), key);
        public static PropertyAccess Get(this Variable v, Variable el) => new PropertyAccess(v.Ref(), el.Id);
        public static PropertyAccess Get(this VariableRef vr, Identifier key) => new PropertyAccess(vr, key);
        public static PropertyAccess Get(this PropertyAccess pa, Identifier key) => new PropertyAccess(pa, key);
        public static VariableRef Ref(this Variable v) => new(v.Id);

        public static StructNew New(this StructType t, IEnumerable<Assignment> args) => new(t.Id, args.ToList());
    }

}
