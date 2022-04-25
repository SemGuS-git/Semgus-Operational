using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    internal interface ISynaxMatchingFrame : IEnumerator<Symbol> {
        public void NotifySuccess(IEnumerable<ISyntaxNode> ok);
        public void NotifyFailure();
        bool IsSuccess { get; }
        int Cursor { get; set; }

        IEnumerable<ISyntaxNode> Bake();
    }
}
