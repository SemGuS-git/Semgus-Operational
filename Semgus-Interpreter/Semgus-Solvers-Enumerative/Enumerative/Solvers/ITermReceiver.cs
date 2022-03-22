using Semgus.Operational;
using System;

namespace Semgus.Solvers.Enumerative {
    public interface ITermReceiver {
        TermReceiverCode Receive(IDSLSyntaxNode node);
    }
}
