using System.Text;

namespace Semgus.OrderSynthesis {
    internal class SketchStdStreamReceiver : IStdStreamReceiver {

        public bool Rejected { get; set; } = false;

        private StringBuilder StdoutLines { get; } = new();

        public string GetResult() => StdoutLines.ToString();

        public void Receive(StdStreamName tag, string line) {
            switch(tag) {
                case StdStreamName.Stdout:
                    StdoutLines.AppendLine(line);
                    break;
                case StdStreamName.Stderr:
                    Console.WriteLine($"sketch stderr :: {line}");
                    if (!Rejected && line == "*** Rejected") {
                        Console.WriteLine($"sketch saw rejection");
                        Rejected = true;
                    }
                    break;
            }
        }

        public void Done() {
            Console.WriteLine($"  sketch done");
        }
    }
}
