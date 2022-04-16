using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis {
    class FunctionNamespace {
        public Dictionary<string, ISettable> VarMap { get; } = new();


        public IExpression Convert(ISmtLibExpression expression) => expression switch {
            VariableEvalExpression varEval => VarMap[varEval.Variable.Name],
            LiteralExpression lit => new Literal(lit.BoxedValue),
            FunctionCallExpression call => ConvertCall(call),
            _ => throw new NotSupportedException()
        };

        private IExpression ConvertCall(FunctionCallExpression call) {
            // Special cases
            switch (call.Function.Name) {
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

            if (GetInfixOpOrNull(call.Function.Name) is Op op) {
                return new InfixOperation(op, call.Args.Select(Convert).ToList());
            }

            if (LibFunctions.MapSmtOrNull(call.Function.Name) is Identifier id) {
                return new FunctionEval(id, call.Args.Select(Convert).ToList());
            }

            throw new KeyNotFoundException($"Expression includes unmapped SMT function \"{call.Function.Name}\"");
        }

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