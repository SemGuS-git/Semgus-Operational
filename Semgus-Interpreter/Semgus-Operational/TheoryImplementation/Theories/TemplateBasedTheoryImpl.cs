using Semgus.Model.Smt;
using System.Diagnostics.CodeAnalysis;

namespace Semgus.Interpretation {
    public abstract class TemplateBasedTheoryImpl : ITheoryImplementation {
        private readonly IReadOnlyDictionary<SmtIdentifier, List<FunctionTemplate>> _templatesByName;

        public TemplateBasedTheoryImpl(IEnumerable<FunctionTemplate> templates) {
            var dict = new Dictionary<SmtIdentifier, List<FunctionTemplate>>();

            List<FunctionTemplate> GetListAt(SmtIdentifier identifier) {
                if (dict.TryGetValue(identifier, out var list)) return list;
                var next = new List<FunctionTemplate>();
                dict.Add(identifier, next);
                return next;
            }

            foreach (var template in templates) {
                GetListAt(template.Identifier).Add(template);
            }

            _templatesByName = dict;
        }

        public bool TryGetFunction(SmtFunction def, SmtFunctionRank rank, [NotNullWhen(true)] out FunctionInstance? fn) {
            if(!_templatesByName.TryGetValue(def.Name,out var list)) {
                fn = default;
                return false;
            }
            var any = false;
            FunctionInstance? found = default;

            foreach(var opt in list) {
                if(opt.Validate(rank)) {
                    if(any) {
                        throw new Exception("Ambiguous theory function match");
                    } else {
                        any = true;
                        found = opt.GetInstance(rank);
                    }
                }
            }
            fn = found;
            return any;
        }
    }
}