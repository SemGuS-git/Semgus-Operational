namespace Semgus.OrderSynthesis {
    internal class SketchStdStreamReceiver : IStdStreamReceiver {
        public bool Rejected { get; set; } = false;
        public void Receive(StdStreamName tag, string line) {
            Console.WriteLine($"  wsl.{tag} :: {line}");
            if (!Rejected && line == "*** Rejected") {
                Console.WriteLine($"  wsl sketch saw rejection");
                Rejected = true;
            }
        }
        public void Done() {
            Console.WriteLine($"  wsl sketch done");
        }
    }
}
