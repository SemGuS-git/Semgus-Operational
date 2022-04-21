namespace Semgus.MiniParser {

    internal interface IToken {
        bool Is(string s) => false;
        bool Is(char s) => false;

        bool IsAnyOf(HashSet<string> s, out string? which) {
            which = null;
            return false;
        }
    }
}
