using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax {

    internal record FunctionEval  (Identifier Id, IReadOnlyList<IExpression> Args)  : IExpression, IStatement  {
        public FunctionEval(Identifier id, params IExpression[] args) : this(id, args.ToList()) { }

        public override string ToString() => $"{Id}({string.Join(", ", Args)})";

        
        public bool TryMakeInline(FunctionDefinition fn, out IEnumerable<IStatement> statements) {
            var sig = fn.Signature;
            Debug.Assert(sig.Id == Id);
            Debug.Assert(sig.Args.Count == Args.Count);

            if(sig.ReturnTypeId != VoidType.Id) {
                statements = Array.Empty<IStatement>();
                return false;
            }

            List<IStatement> lines = new();

            List<(IVariableInfo out_var, ISettable target)> outVarMap = new();

            foreach((var param,var value) in sig.Args.Zip(Args)) {
                if(param is RefVariableDeclaration refParam) {
                    outVarMap.Add((param, (ISettable)value));
                } else {
                    lines.Add(new WeakVariableDeclaration(param.TypeId, param.Id, value));
                }
            }

            foreach(var st in fn.Body) {
                if (st is ReturnStatement r) {
                    if(r.Expr is not null) {
                        statements = Array.Empty<IStatement>();
                        return false;
                    }
                    break;
                } else {
                    lines.Add(st);
                }
            }

            foreach((var out_var, var target) in outVarMap) {
                lines.Add(target.Assign(new VariableRef(out_var.Id)));
            }

            statements = lines;
            return true;
        }

        public virtual bool Equals(FunctionEval? other) => other is not null && Id.Equals(other.Id) && Args.SequenceEqual(other.Args);

        public void WriteInto(ILineReceiver lineReceiver) => lineReceiver.Add(ToString() + ";");
    }
}
