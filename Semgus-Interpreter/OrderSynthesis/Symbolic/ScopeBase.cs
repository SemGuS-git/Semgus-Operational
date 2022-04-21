using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal record StructValuePlaceholder(IReadOnlyList<Identifier> PropertyKeys) : IExpression {

    }

    internal abstract class ScopeBase : IScope {
        protected Dictionary<Identifier, IExpression> LocalAssigns { get; } = new();
        protected HashSet<Identifier> LocalDefines { get; } = new();
        public Stack<ExpressionScope> PendingStack { get; } = new();
        public IEnumerator<IStatement> Enumerator { get; }

        public ScopeBase(IEnumerable<IStatement> statements) {
            Enumerator = statements.GetEnumerator();
        }

        public virtual bool TryGetLocalValue(Identifier identifier, out IExpression value) => LocalAssigns.TryGetValue(identifier, out value);

        public void Declare(Identifier id, IExpression expression) {
            if (expression is StructNew struct_value) {

                List<Identifier> child_keys = new();
                foreach (var m in struct_value.Args) {
                    var flat_id = new Identifier(string.Join('.', id, ((VariableRef)m.Subject).TargetId));
                    child_keys.Add(flat_id);
                    Declare(flat_id, m.Value);
                }
                Declare(id, new StructValuePlaceholder(child_keys));

            } else {
                if (!LocalDefines.Add(id)) throw new InvalidOperationException("Redefinition of variable in scope");
                if (!LocalAssigns.TryAdd(id, expression)) throw new InvalidOperationException("Variable was assigned before definition in scope");
            }
        }

        public void Assign(Identifier id, IExpression expression) {
            if (expression is StructNew struct_value) {
                List<Identifier> child_keys = new();

                foreach (var m in struct_value.Args) {
                    var flat_id = new Identifier(string.Join('.', id, ((VariableRef)m.Subject).TargetId));
                    child_keys.Add(flat_id);
                    Assign(flat_id, m.Value);
                }

                LocalAssigns[id] = new StructValuePlaceholder(child_keys);
            } else {
                LocalAssigns[id] = expression;
            }
        }

        public void Assign(ISettable subject, IExpression expression) {
            switch(subject) {
                case VariableRef v:
                    Assign(v.TargetId, expression);
                    break;
                case PropertyAccess p:
                    var key_stack = new Stack<Identifier>();

                    var access = p;
                    while (access.Expr is PropertyAccess inner_access) {
                        key_stack.Push(access.Key);
                        access = inner_access;
                    }

                    if (access.Expr is not VariableRef core) throw new NotSupportedException();

                    key_stack.Push(access.Key);
                    key_stack.Push(core.TargetId);

                    Assign(new Identifier(string.Join('.', key_stack)), expression);
                    break;
                default: throw new NotSupportedException();
            }
        }

        public IEnumerable<KeyValuePair<Identifier, IExpression>> GetSideEffectAssigns() {
            return LocalAssigns.Where(kvp => !LocalDefines.Contains(kvp.Key));
        }
        public abstract void OnPop(ScopeStack stack);
    }

}
