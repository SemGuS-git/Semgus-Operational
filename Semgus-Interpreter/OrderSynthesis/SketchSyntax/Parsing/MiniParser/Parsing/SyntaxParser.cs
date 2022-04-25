using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    internal class SyntaxParser {
        private readonly AccumulatorTape<IToken> tokens;
        int tokenCursor = 0;

        public SyntaxParser(IEnumerator<IToken> tokenStream) {
            this.tokens = new(tokenStream);
        }

        record Frame(Symbol symbol, int Cursor) { }

        public bool TryParse(Symbol root, out IEnumerable<ISyntaxNode> result) {
            throw new NotImplementedException("Non-recursive parser isn't validated right now");
            var stack = new Stack<ISynaxMatchingFrame>();

            if (root is INonTerminalSymbol root_nt) {
                stack.Push(root_nt.GetFrame());
            } else {
                if (!tokens.InBounds(1) && tokens[0].TryGetValue(out var root_tk) && root.CheckTerminal(root_tk, out var first)) {
                    result = new[] { first };
                    return true;
                } else {
                    result = Enumerable.Empty<ISyntaxNode>();
                    return false;
                }
            }

            while (stack.TryPeek(out var frame)) {
                if (frame.MoveNext()) {
                    var symbol = frame.Current;

                    if (symbol is INonTerminalSymbol nt) {
                        var next_frame = nt.GetFrame();
                        next_frame.Cursor = frame.Cursor;
                        stack.Push(next_frame);
                        continue;
                    }

                    if (!tokens[frame.Cursor].TryGetValue(out var token)) break;

                    if (symbol.CheckTerminal(token, out var node)) {
                        frame.Cursor++;
                        frame.NotifySuccess(new[] { node });
                    } else {
                        frame.NotifyFailure();
                    }
                    continue;
                }

                if (frame.IsSuccess) {
                    var baked = frame.Bake();
                    {
                        if (stack.TryPeek(out var parent)) {
                            parent.Cursor = frame.Cursor;
                            parent.NotifySuccess(baked);
                            continue;
                        }
                        result = baked;
                        return true;
                    }
                }
                {
                    if (stack.TryPeek(out var parent)) {
                        parent.NotifyFailure();
                        continue;
                    }
                }

                break;
            }
            result = Enumerable.Empty<ISyntaxNode>();
            return false;

        }
    }
}
