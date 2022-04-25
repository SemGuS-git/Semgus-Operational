namespace Semgus.MiniParser {
    internal class InvalidTokenException: Exception {
        public InvalidTokenException(string text, int a, int b) : base($"Failed to scan token \"{text[a..b]}\"   (recent: \"{text[Math.Max(0,a-10)..a]}\"") {
            this.Text = text;
        }
        public InvalidTokenException(string text, int a, int b, string msg) : base($"{msg} \"{text[a..b]}\"   (recent: \"{text[Math.Max(0, a - 10)..a]}\"") {
            this.Text = text;
        }

        public string Text { get; private set; }
    }
}
