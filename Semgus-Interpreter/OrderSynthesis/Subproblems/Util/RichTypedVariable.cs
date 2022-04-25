using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record RichTypedVariable(Identifier Id, IType Type) {
        public RichTypedVariable(string name, IType type) : this(new Identifier(name), type) { }

        public override string ToString() => $"{Type.Id} {Id}";

        public Variable Sig() => new(Id, Type.Id);
        public VariableRef Ref() => new(Id);
    }
}
