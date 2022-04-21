using Semgus.MiniParser;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class RootScope : InvocationScope {
        private readonly HashSet<Identifier> _refVariables;


        private RootScope(FunctionDefinition fn, IReadOnlyList<VariableRef> argRefs) : base(fn, argRefs, argRefs) {
            this._refVariables = fn.Signature.Args.Where(a => a is RefVariableDeclaration).Select(a => a.Id).ToHashSet();
        }

        public RootScope(FunctionDefinition fn) : this(fn, fn.Signature.Args.Select(vi => new VariableRef(vi.Id)).ToList()) { }


        public SymbolicInterpreter.Result? Result { get; private set; } = null;

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);

            if (LocalAssigns.TryGetValue(ReturnValueId, out var returnValue) && returnValue is not Empty) {
                Debug.Assert(!_isVoid);
            } else {
                Debug.Assert(_isVoid);
                returnValue = Empty.Instance;
            }

            Dictionary<Identifier, IExpression> refVars = new(), globals = new();

            foreach (var assigned in LocalAssigns) {
                if (_refVariables.Contains(assigned.Key)) {
                    refVars.Add(assigned.Key, assigned.Value);
                } else if (!LocalDefines.Contains(assigned.Key)) {
                    globals.Add(assigned.Key, assigned.Value);
                }
            }

            Result = new(returnValue, refVars, globals);
        }
    }

}