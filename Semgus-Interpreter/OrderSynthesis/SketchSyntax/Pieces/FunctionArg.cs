using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record FunctionArg(Variable Variable, bool IsRef = false) : ISyntaxNode, ISettable {
        public FunctionArg(Identifier id, Identifier typeId, bool isRef = false) : this(new(id, typeId), isRef) { }

        public Identifier Id => Variable.Id;
        public Identifier TypeId => Variable.TypeId;

        public override string ToString() => Variable.Id.ToString();
    }
}
