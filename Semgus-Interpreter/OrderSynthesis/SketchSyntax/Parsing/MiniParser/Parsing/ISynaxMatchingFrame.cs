using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    internal interface ISynaxMatchingFrame : IEnumerator<Symbol> {
        public void NotifySuccess(IEnumerable<INode> ok);
        public void NotifyFailure();
        bool IsSuccess { get; }
        int Cursor { get; set; }

        IEnumerable<INode> Bake();
    }
}
