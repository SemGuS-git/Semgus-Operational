using System.Text;

namespace Semgus.OrderSynthesis {
    internal interface ILineReceiver {

        void IndentIn();
        void IndentOut();

        void Add(string line);
    }
}