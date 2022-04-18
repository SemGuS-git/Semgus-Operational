using Semgus.OrderSynthesis.SketchSyntax;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class MonotoneLabeling {
        public FunctionDefinition Function { get; }
        public IReadOnlyList<Monotonicity> ArgMonotonicities { get; }

        public MonotoneLabeling(FunctionDefinition function, IReadOnlyList<Monotonicity> argMonotonicities) {
            if (function.Signature.Args.Count != argMonotonicities.Count) throw new ArgumentException();
            Function = function;
            ArgMonotonicities = argMonotonicities;
        }
    }
}
