namespace Semgus.OrderSynthesis {
    internal interface IStdStreamReceiver {
        void Receive(StdStreamName tag, string line);
        void Done();
    }
}
