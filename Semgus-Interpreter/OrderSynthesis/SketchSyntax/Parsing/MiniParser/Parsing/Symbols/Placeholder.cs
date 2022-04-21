using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.MiniParser {
    internal class Placeholder : Symbol, INonTerminalSymbol {

        public override string? Name {
            get { return Inner.Name; }
            set { Inner.Name = value; }
        }
        public Symbol? Inner { get; private set; }

        internal void Install(Symbol inner) => Inner = inner;

        public override bool CheckTerminal(IToken token, out INode node) => Inner.CheckTerminal(token, out node);

        public ISynaxMatchingFrame GetFrame() => ((INonTerminalSymbol)Inner).GetFrame();

        public override string ToString() => Inner?.ToString()??"<placeholder>";

        internal override Result<IEnumerable<INode>, ParseError> ParseRecursive(TapeEnumerator<IToken> tokens) => Inner!.ParseRecursive(tokens);
    }
}
