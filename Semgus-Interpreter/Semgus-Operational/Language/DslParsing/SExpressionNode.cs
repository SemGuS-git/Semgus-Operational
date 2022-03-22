using System.Text;

namespace Semgus.Operational {
    public class SExpressionNode {
        public string Symbol { get; }

        public List<SExpressionNode> children = new List<SExpressionNode>();

        public SExpressionNode(string symbol) {
            Symbol = symbol;
        }

        private void ToString(StringBuilder sb) {
            if(children.Count == 0) {
                sb.Append(Symbol);
            } else {
                sb.Append('(');
                sb.Append(Symbol);
                foreach(var ch in children) {
                    sb.Append(' ');
                    ch.ToString(sb);
                }
                sb.Append(')');
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }


        public static SExpressionNode Parse(string s) {
            using var sr = new StringReader(s);
            return Parse(sr);
        }

        public static SExpressionNode Parse(TextReader reader) {
            int x;
            char c;
            var builder = new StringBuilder();

            var stack = new Stack<SExpressionNode>();

            bool parenFlag = false;
            int d = 0;

            void EndOfSymbol() {
                if (builder.Length == 0) return;

                var n = new SExpressionNode(builder.ToString());
                builder.Clear();

                if (parenFlag) {
                    stack.Push(n);
                    parenFlag = false;
                } else {
                    if (stack.Count == 0) stack.Push(n);
                    else stack.Peek().children.Add(n);
                }
            }

            while (true) {
                x = reader.Read();
                if (x == -1) break;
                c = (char)x;

                if (char.IsWhiteSpace(c)) {
                    EndOfSymbol();
                    continue;
                }

                switch (c) {
                    case '(':
                        d++;
                        EndOfSymbol();
                        if (parenFlag) throw new Exception();
                        parenFlag = true;
                        break;
                    case ')':
                        d--;
                        EndOfSymbol();
                        var t = stack.Pop();
                        if (stack.Count == 0) return t;
                        stack.Peek().children.Add(t);
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            EndOfSymbol();
            if (d != 0) throw new Exception();
            if (stack.Count == 1) return stack.Peek();
            throw new Exception();
        }
    }
}