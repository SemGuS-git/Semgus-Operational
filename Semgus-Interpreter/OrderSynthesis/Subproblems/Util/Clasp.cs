using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record Clasp(StructType Type, IReadOnlyList<RichTypedVariable> Indexed, RichTypedVariable Alternate) {
        public static IReadOnlyList<Clasp> GetAll(IReadOnlyList<StructType> types, IReadOnlyDictionary<Identifier,StructType> lookup, IEnumerable<FunctionSignature> signatures) {
            Dictionary<Identifier, int> nvar = new();
            //HashSet<StructType> participants = new();
            foreach (var sig in signatures) {
                if (!lookup.TryGetValue(sig.ReturnTypeId,out var return_type)) throw new NotSupportedException();

                if (!nvar.ContainsKey(return_type.Id)) {
                    nvar[return_type.Id] = 3;
                    //participants.Add(return_type);
                }

                Counter<Identifier> vcounts = new();
                foreach (var arg in sig.Args) {

                    if (!lookup.TryGetValue(arg.TypeId,out var arg_type)) throw new NotSupportedException();
                    var arg_type_id = arg_type.Id;

                    vcounts.Increment(arg_type_id);
                    if (!nvar.ContainsKey(arg_type_id)) {
                        nvar[arg.TypeId] = 3;
                        //participants.Add(arg_type);
                    }
                }

                foreach (var kvp in vcounts) {
                    if (nvar[kvp.Key] < kvp.Value) nvar[kvp.Key] = kvp.Value;
                }
            }

            return types.Where(t=>nvar.ContainsKey(t.Id)).Select(p =>
                new Clasp(
                    p,
                    Enumerable.Range(0, nvar[p.Id]).Select(i => new RichTypedVariable($"{p.Name}_s{i}", p)).ToList(),
                    new RichTypedVariable($"{p.Name}_alt", p)
                 )
            ).ToList();
        }
    }
}
