namespace Semgus.OrderSynthesis.SketchSyntax {

    internal interface IStatement : ISyntaxNode {
        void WriteInto(ILineReceiver lineReceiver);
    }
}
