using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.MiniParser {
    abstract class FrameBase : ISynaxMatchingFrame {
        public bool IsSuccess { get; protected set; } = false;
        public int Cursor { get; set; }

        public abstract Symbol Current { get; }

        object System.Collections.IEnumerator.Current => Current;


        public abstract IEnumerable<ISyntaxNode> Bake();
        public abstract void NotifyFailure();

        public abstract void NotifySuccess(IEnumerable<ISyntaxNode> ok);

        public abstract bool MoveNext();

        public virtual void Reset() => throw new NotSupportedException();

        public virtual void Dispose() { }
    }
}
