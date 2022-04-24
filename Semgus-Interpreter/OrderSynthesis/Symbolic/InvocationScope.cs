using Semgus.MiniParser;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class InvocationScope : ScopeBase {
        public static Identifier ReturnValueId { get; } = new(";out");

        readonly Dictionary<VariableRef, Identifier> outer_to_arg_ids = new();

        protected readonly bool _isVoid;

        public InvocationScope(FunctionDefinition def) : base(def.Body) {
            _isVoid = def.Signature.ReturnTypeId == VoidType.Id;
        }

        public virtual void Initialize(FunctionDefinition def, IReadOnlyList<IExpression> arg_raw_expressions, IReadOnlyList<IExpression> arg_values) {
            int n = def.Signature.Args.Count;

            for (int i = 0; i < n; i++) {
                var arg = def.Signature.Args[i];

                if (arg is RefVariableDeclaration ref_arg) {
                    // We expect all struct prop accesses to be flattened
                    if (arg_raw_expressions[i] is VariableRef settable) {
                        outer_to_arg_ids.Add(settable, ref_arg.Id);
                    } else {
                        throw new ArgumentException();
                    }
                }

                var value = arg_values[i];
                Declare(arg.Id,value);
            }

            Declare(ReturnValueId, Empty.Instance);
        }

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);

            var parent = stack.Peek();

            // Unflatten locally assigned / defined struct values
            BakeAllStructVars();

            // Set ref variables in parent scope
            foreach (var (settable, local_var_id) in outer_to_arg_ids) {
                parent.Assign(settable.TargetId, LocalAssigns[local_var_id]);
            }

            // Note: anything that was set but not defined must be a global variable
            foreach (var kvp in GetSideEffectAssigns()) {
                parent.Assign(kvp.Key, kvp.Value);
            }

            if (LocalAssigns.TryGetValue(ReturnValueId, out var returnValue) && returnValue is not Empty) {
                Debug.Assert(!_isVoid);
            } else {
                Debug.Assert(_isVoid);
                returnValue = Empty.Instance;
            }

            parent.ReceiveExpression(returnValue);
        }
    }
}