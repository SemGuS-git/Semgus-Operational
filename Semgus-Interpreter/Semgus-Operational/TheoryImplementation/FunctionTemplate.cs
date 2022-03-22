using Semgus.Model.Smt;

namespace Semgus.Operational {


    public class FunctionTemplate {
        public delegate bool SignatureValidator(SmtFunctionRank rank);
        public delegate SmtSort? ReturnSortSuggestor(IReadOnlyList<SmtSort> sorts);

        public delegate FunctionInstance.Evaluator Instantiator(SmtFunctionRank rank);
        private readonly Dictionary<SmtFunctionRank, FunctionInstance> _instances = new();

        public SmtIdentifier Identifier { get; }
        public ReturnSortSuggestor SuggestReturnSort { get; } // Suggest a return type that might create a valid signature for this function with the given arguments. Returns null if no suggestion can be made.
        public SignatureValidator Validate { get; }
        private Instantiator Instantiate { get; }

        public FunctionTemplate(SmtIdentifier identifier, ReturnSortSuggestor suggestReturnSort, SignatureValidator validate, Instantiator instantiate) {
            Identifier = identifier;
            SuggestReturnSort = suggestReturnSort;
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
