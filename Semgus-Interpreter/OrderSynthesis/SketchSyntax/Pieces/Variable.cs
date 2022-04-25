using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Variable(Identifier Id, Identifier TypeId) : ISyntaxNode {
        public Variable(string name, Identifier typeId) : this(new Identifier(name), typeId) { }
        public Variable(string name, IType type) : this(new Identifier(name), type.Id) { }

        public override string ToString() => $"{TypeId} {Id}";
    }
}
