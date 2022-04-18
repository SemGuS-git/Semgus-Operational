namespace Semgus.OrderSynthesis.SketchSyntax {

    internal interface IStatement : INode {
        void WriteInto(ILineReceiver lineReceiver);
    }
}
