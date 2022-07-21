using Semgus.OrderSynthesis.IntervalSemantics;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    class FunctionNamespace {
        public Dictionary<BlockItemRef, ISettable> VarMap { get; }

        public FunctionNamespace(IEnumerable<(FunctionArg, StructType)> args) {
            var var_map = new Dictionary<BlockItemRef, ISettable>();

            int i = 0;
            foreach (var arg in args) {
                for (int j = 0; j < arg.Item2.Elements.Count; j++) {
                    var item = arg.Item2.Elements[j];
                    var_map.Add(new(i, j), arg.Item1.Variable.Get(item.Id));
                }
                i++;
            }

            VarMap = var_map;
        }

        public IExpression Convert(IBlockExpression expression) => expression switch {
            BlockExprRead read => VarMap[read.Address],
            BlockExprLiteral lit => new Literal(lit.Value),
            BlockExprCall call => ConvertCall(call),
            _ => throw new NotSupportedException()
        };

        private IExpression ConvertCall(BlockExprCall call) {
            // Special cases
            switch (call.FunctionName) {
                case "ite":
                    if (call.Args.Count != 3) throw new InvalidDataException();
                    return new Ternary(Convert(call.Args[0]), Convert(call.Args[1]), Convert(call.Args[2]));
                case "true":
                    if (call.Args.Count != 0) throw new InvalidDataException();
                    return new Literal(1);
                case "false":
                    if (call.Args.Count != 0) throw new InvalidDataException();
                    return new Literal(0);
            }

            if (call.Args.Count == 1 && GetUnaryOpOrNull(call.FunctionName) is UnaryOp un_op) {
                return new UnaryOperation(un_op, Convert(call.Args[0]));
            }

            if (call.Args.Count > 1 && GetInfixOpOrNull(call.FunctionName) is Op op) {
                return new InfixOperation(op, call.Args.Select(Convert).ToList());
            }

            throw new KeyNotFoundException($"Expression includes unmapped SMT function \"{call.FunctionName}\"");
        }

        private static UnaryOp? GetUnaryOpOrNull(string name) => name switch {
            "not" => UnaryOp.Not,
            "-" => UnaryOp.Minus,
            _ => null,
        };

        private static Op? GetInfixOpOrNull(string smtFn) => smtFn switch {
            "=" => Op.Eq,
            "!=" => Op.Neq,
            "and" => Op.And,
            "or" => Op.Or,
            "+" => Op.Plus,
            "-" => Op.Minus,
            "*" => Op.Times,
            "<" => Op.Lt,
            ">" => Op.Gt,
            "<=" => Op.Leq,
            ">=" => Op.Geq,
            _ => null,
        };
    }

}
