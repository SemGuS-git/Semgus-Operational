namespace Semgus.OrderSynthesis.SketchSyntax {
    internal interface IStatement {
        void WriteInto(ILineReceiver lineReceiver);
    }
}
