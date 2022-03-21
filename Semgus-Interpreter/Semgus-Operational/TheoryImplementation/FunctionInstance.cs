namespace Semgus.Interpretation {
    public class FunctionInstance {
        public delegate object Evaluator(object[] args);

        public string Name { get; }
        public Evaluator Evaluate { get; }

        public FunctionInstance(string name, Evaluator evaluate) {
            Name = name;
            Evaluate = evaluate;
        }
    }
}
