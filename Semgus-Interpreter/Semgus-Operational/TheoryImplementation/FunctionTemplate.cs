using Semgus.Model.Smt;

namespace Semgus.Interpretation {
    public class FunctionTemplate {
        public delegate bool SignatureValidator(SmtFunctionRank rank);
        public delegate FunctionInstance.Evaluator Instantiator(SmtFunctionRank rank);

        private readonly Dictionary<SmtFunctionRank, FunctionInstance> _instances = new();

        public SmtIdentifier Identifier { get; }
        public SignatureValidator Validate { get; }
        private Instantiator Instantiate { get; }

        public FunctionTemplate(SmtIdentifier identifier, SignatureValidator validate, Instantiator instantiate) {
            Identifier = identifier;
            Validate = validate;
            Instantiate = instantiate;
        }

        internal FunctionInstance GetInstance(SmtFunctionRank rank) {
            if(!_instances.TryGetValue(rank,out var value)) {
                value = new(Identifier.Symbol, Instantiate(rank));
                _instances.Add(rank, value);
            }
            return value;
        }
    }
}
