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
    }
}
