namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class ExpressionScope {
        public IEnumerator<IExpression> RawExpressions { get; }
        private INode Source { get; }
        private List<IExpression> FlattenedTerms { get; } = new();

        public ExpressionScope(INode source, IEnumerable<IExpression> raw_expressions) {
            RawExpressions = raw_expressions.GetEnumerator();
            Source = source;
        }
        public ExpressionScope(INode source, params IExpression[] raw_expressions) : this(source, (IEnumerable<IExpression>)raw_expressions) { }

        public void AddExpression(IExpression expression) => FlattenedTerms.Add(expression);

        public void Finalize(ScopeStack stack, IScope frame) {
            switch (Source) {
                // Expressions
                case Ternary:
                    frame.ReceiveExpression(new Ternary(FlattenedTerms[0], FlattenedTerms[1], FlattenedTerms[2]));
                    break;
                case FunctionEval _invoc: // may also be inserted as a statement
                    if (stack.TryGetFunction(_invoc.Id, out var function)) {
                        var invo = new InvocationScope(function);
                        invo.Initialize(function, _invoc.Args, FlattenedTerms);
                        stack.Push(invo);
                    } else {
                        frame.ReceiveExpression(new FunctionEval(_invoc.Id, FlattenedTerms));
                    }
                    break;
                case UnaryOperation _unary:
                    frame.ReceiveExpression(new UnaryOperation(_unary.Op, FlattenedTerms[0]));
                    break;
                case InfixOperation _infix:
                    frame.ReceiveExpression(new InfixOperation(_infix.Op, FlattenedTerms));
                    break;
                case PropertyAccess _prop:
                    frame.ReceiveExpression(FlattenedTerms[0] switch {
                        // e.g. global_variable.x
                        VariableRef variable
                            => stack.Resolve(new(string.Join('.', variable.TargetId, _prop.Key))),

                        // e.g. input_variable.x
                        StructValuePlaceholder placeholder
                            => stack.TryGetAssignedValue(new($"{placeholder.Id}.{_prop.Key}"), out var overwrite)
                            ? overwrite
                            : placeholder.Source.TryGetPropValue(_prop.Key, out var value) ? value : throw new KeyNotFoundException(),

                        // e.g. (new Pair(5,3)).x
                        StructNew anonymous_struct_value
                            => anonymous_struct_value.TryGetPropValue(_prop.Key,out var value) ? value : throw new KeyNotFoundException(),

                        // e.g. (new Pair(1,2) + new Pair(3,5)).x
                        _ => throw new NotSupportedException(),
                    });
                    break;

                // Statements
                case IfStatement _if:
                    stack.Push(new LeftBranch(FlattenedTerms[0], _if.BodyLhs, _if.BodyRhs));
                    break;
                case ReturnStatement:
                    frame.Assign(InvocationScope.ReturnValueId, FlattenedTerms.FirstOrDefault(Empty.Instance));
                    stack.Pop().OnPop(stack); // Halt execution in this branch
                    break;
                case VariableDeclaration vd:
                    frame.Declare(vd.Var.Id, FlattenedTerms[0]);
                    break;
                case WeakVariableDeclaration wvd:
                    frame.Declare(wvd.Id, FlattenedTerms[0]);
                    break;
                case Assignment asn:
                    if (asn.Subject is not VariableRef flat) throw new ArgumentException("All assignment targets must be flattened during symbolic evaluation");
                    frame.Assign(flat.TargetId, FlattenedTerms[0]);
                    break;
            }
        }

        public static ExpressionScope From(IStatement statement) => statement switch {
            IfStatement _if => (new(_if, _if.Condition)),
            ReturnStatement _ret => (new(_ret, _ret.Expr)),
            VariableDeclaration vd => (new(vd, vd.Def)),
            WeakVariableDeclaration wvd => (new(wvd, wvd.Def)),
            Assignment asn => (new(asn, asn.Value)),
            FunctionEval _call => (new(_call, _call.Args)),
            FunctionDefinition
            or StructDefinition
            or AssertStatement
            or MinimizeStatement
            or RepeatStatement
            or _ => throw new NotSupportedException(),
        };

    }
}