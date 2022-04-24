using Semgus.MiniParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace Semgus.OrderSynthesis.SketchSyntax.Helpers {
    internal static class Sugar {
        public static Literal Lit0 { get; } = new Literal(0);
        public static Literal Lit1 { get; } = new Literal(1);
        public static Literal Lit2 { get; } = new Literal(2);
        public static Literal Lit3 { get; } = new Literal(3);
        public static Literal Lit4 { get; } = new Literal(4);
        public static Literal Lit5 { get; } = new Literal(5);
        public static Literal Lit6 { get; } = new Literal(6);
        public static Literal Lit7 { get; } = new Literal(7);

        public static InfixOperation Bi(IExpression lhs, Op op, IExpression rhs) => new(op, lhs, rhs);
        public static InfixOperation Bi(Variable lhs, Op op, Variable rhs) => new(op, lhs.Ref(), rhs.Ref());

        public static IfStatement If(IExpression condition, params IStatement[] body) => new(condition, body);
        public static IfStatement If(IExpression condition, IReadOnlyList<IStatement> body) => new(condition, body);

        public static ReturnStatement Return() => new();
        public static ReturnStatement Return(IExpression v) => new(v);

        public static AssertStatement Assertion(IExpression v) => new(v);

        public static UnaryOperation Not(IExpression v) => new(UnaryOp.Not, v);
    }

    internal static class SketchSyntaxExtensions {
        public static IfStatement IfEq(this IExpression cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs, cond_rhs), body);
        public static IfStatement IfEq(this Variable cond_lhs, IExpression cond_rhs, params IStatement[] body) => new(new InfixOperation(Op.Eq, cond_lhs.Ref(), cond_rhs), body);

        public static IfStatement Else(this IfStatement stmt, params IStatement[] bodyRhs) => stmt.BodyRhs.Count switch {
            0 => stmt with { BodyRhs = bodyRhs },
            1 when stmt.BodyRhs[0] is IfStatement _elseif => stmt with { BodyRhs = new[] { _elseif.Else(bodyRhs) } },
            _ => throw new ArgumentException(),
        };

        public static IfStatement ElseIf(this IfStatement stmt, IExpression cond, params IStatement[] bodyRhs) => stmt.BodyRhs.Count switch {
            0 => stmt with { BodyRhs = new[] { new IfStatement(cond, bodyRhs) } },
            1 when stmt.BodyRhs[0] is IfStatement _elseif => stmt with { BodyRhs = new[] { _elseif.ElseIf(cond, bodyRhs) } },
            _ => throw new ArgumentException(),
        };

        public static InfixOperation Implies(this IExpression lhs, IExpression rhs) => new(Op.Or, new UnaryOperation(UnaryOp.Not, lhs), rhs);
        public static InfixOperation ImpliedBy(this IExpression lhs, IExpression rhs) => new(Op.Or, lhs, new UnaryOperation(UnaryOp.Not, rhs));
        public static InfixOperation Implies(this Variable lhs, Variable rhs) => new(Op.Or, new UnaryOperation(UnaryOp.Not, lhs.Ref()), rhs.Ref());
        public static InfixOperation ImpliedBy(this Variable lhs, Variable rhs) => new(Op.Or, lhs.Ref(), new UnaryOperation(UnaryOp.Not, rhs.Ref()));


        public static FunctionEval Call(this FunctionDefinition fn, Variable arg0, params Variable[] args) => new(fn.Id, args.Prepend(arg0).Select(a => a.Ref()).ToList());
        public static FunctionEval Call(this FunctionDefinition fn, params IExpression[] args) => new(fn.Id, args);
        public static FunctionEval Call(this FunctionDefinition fn, IReadOnlyList<IExpression> args) => new(fn.Id, args);
        public static FunctionEval Call(this Identifier fn_id, Variable arg0, params Variable[] args) => new(fn_id, args.Prepend(arg0).Select(a => a.Ref()).ToList());
        public static FunctionEval Call(this Identifier fn_id, params IExpression[] args) => new(fn_id, args);
        public static FunctionEval Call(this Identifier fn_id, IReadOnlyList<IExpression> args) => new(fn_id, args);

        public static FunctionEval Compare(this StructType st, IExpression lhs, IExpression rhs) => new(st.CompareId, lhs, rhs);
        public static FunctionEval Compare(this StructType st, Variable lhs, Variable rhs) => new(st.CompareId, lhs.Ref(), rhs.Ref());
        public static UnaryOperation NotCompare(this StructType st, IExpression lhs, IExpression rhs) => new(UnaryOp.Not, new FunctionEval(st.CompareId, lhs, rhs));
        public static UnaryOperation NotCompare(this StructType st, Variable lhs, Variable rhs) => new(UnaryOp.Not, new FunctionEval(st.CompareId, lhs.Ref(), rhs.Ref()));
        public static FunctionEval Equal(this StructType st, IExpression lhs, IExpression rhs) => new(st.EqId, lhs, rhs);
        public static FunctionEval Equal(this StructType st, Variable lhs, Variable rhs) => new(st.EqId, lhs.Ref(), rhs.Ref());
        public static UnaryOperation NotEqual(this StructType st, IExpression lhs, IExpression rhs) => new(UnaryOp.Not, new FunctionEval(st.EqId, lhs, rhs));
        public static UnaryOperation NotEqual(this StructType st, Variable lhs, Variable rhs) => new(UnaryOp.Not, new FunctionEval(st.EqId, lhs.Ref(), rhs.Ref()));

        public static UnaryOperation Of(this UnaryOp op, IExpression operand) => new(op, operand);
        public static UnaryOperation Of(this UnaryOp op, Variable operand) => new(op, operand.Ref());

        public static InfixOperation Of(this Op op, params IExpression[] operands) => new(op, operands);
        public static InfixOperation Of(this Op op, params Variable[] operands) => new(op, operands.Select(Ref).ToList());
        public static InfixOperation Of(this Op op, IReadOnlyList<IExpression> operands) => new(op, operands);
        public static InfixOperation Of(this Op op, IReadOnlyList<Variable> operands) => new(op, operands.Select(Ref).ToList());

        public static Assignment Assign(this Variable subject, IExpression value) => new(subject.Ref(), value);
        public static Assignment Assign(this ISettable subject, IExpression value) => new(subject, value);

        public static VariableDeclaration Declare(this Variable v) => new(v);
        public static VariableDeclaration Declare(this Variable v, IExpression value) => new(v, value);

        public static PropertyAccess Get(this Variable v, Identifier key) => new(v.Ref(), key);
        public static PropertyAccess Get(this Variable v, Variable el) => new(v.Ref(), el.Id);
        public static PropertyAccess Get(this VariableRef vr, Identifier key) => new(vr, key);
        public static PropertyAccess Get(this PropertyAccess pa, Identifier key) => new(pa, key);
        public static VariableRef Ref(this Variable v) => new(v.Id);

        public static StructNew New(this StructType t, IEnumerable<Assignment> args) => new(t.Id, args.ToList());

        public static StructNew NewFromHoles(this StructType t)
            => new(t.Id, t.Elements.Select(
                e => new Assignment(e.Ref(), new Hole())
            ).ToList());


        public static FunctionDefinition RenamedTo(this FunctionDefinition fn, Identifier id) => fn with { Signature = fn.Signature with { Id = id } };
    }
}
