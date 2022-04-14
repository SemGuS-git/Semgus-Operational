namespace Semgus.OrderSynthesis {
    internal class ConsoleStdStreamReceiver : IStdStreamReceiver {
        public static ConsoleStdStreamReceiver Instance { get; } = new();
        private ConsoleStdStreamReceiver() { }
        public virtual void Receive(StdStreamName tag, string line) {
            Console.WriteLine($"  wsl.{tag} :: {line}");
        }

        public virtual void Done() {
            Console.WriteLine($"  wsl done");
        }
    }
}
