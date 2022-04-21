using Semgus.MiniParser;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal class StructNewScope : ScopeBase {

        private readonly Identifier _typeId;

        private readonly List<Identifier> _argIds;

        public StructNewScope(StructNew src) : base(src.Args) {
            _typeId = src.TypeId;
            _argIds = new(src.Args.Cast<VariableRef>().Select(a => a.TargetId));
        }

        public override void OnPop(ScopeStack stack) {
            Debug.Assert(PendingStack.Count == 0);

            // note: does not check that LocalDefines.SetEquals(type.Elements.Select(e => e.Id)));
            Debug.Assert(LocalDefines.SetEquals(_argIds));

            var parent = stack.Peek();

            // note: anything that was set but not defined should be a global variable
            // this may happen if one of the setters calls another function, which then sets the global
            foreach (var kvp in GetSideEffectAssigns()) {
                parent.Assign(kvp.Key, kvp.Value);
            }

            parent.ReceiveExpression(
                new StructNew(
                    _typeId,
                    _argIds.Select(id => new Assignment(new VariableRef(id), LocalAssigns[id])).ToList()
                )
            );
        }
    }
}
