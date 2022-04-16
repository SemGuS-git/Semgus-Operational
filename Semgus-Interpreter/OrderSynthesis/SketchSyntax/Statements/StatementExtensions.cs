using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal static class StatementExtensions {
        public static string PrettyPrint(this IStatement statement, bool compact = false) {
            var sb = new StringBuilder();
            var rcv = new StringLineReceiver(sb,compact);
            statement.WriteInto(rcv);
            return sb.ToString();
        }
    }
}
