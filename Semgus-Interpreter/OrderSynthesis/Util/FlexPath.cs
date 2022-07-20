namespace Semgus.OrderSynthesis {
    internal class FlexPath {
        public string SharedSuffix { get; }
        public string PathWin { get; }
        public string PathWsl { get; }
        public FlexPath(string suffix) {
            SharedSuffix = suffix;
            PathWin = "c:/" + SharedSuffix;
            PathWsl = "/mnt/c/" + SharedSuffix;
        }

        public FlexPath Append(string more) => new(SharedSuffix + more);
        public override string ToString() => "FLEX::" + SharedSuffix;

        public static FlexPath FromWin(string path_win) {
            path_win = path_win.Replace('\\', '/');
            if (path_win[..3].ToLower() != "c:/") throw new ArgumentException();
            return new(path_win[3..]);
        }
    }
}
