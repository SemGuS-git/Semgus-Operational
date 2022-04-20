﻿using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    internal record Clasp(StructType Type, IReadOnlyList<Variable> Indexed, Variable Alternate) {
        public static IReadOnlyList<Clasp> GetAll(IEnumerable<FunctionSignature> functions) {
            Dictionary<Identifier, int> nvar = new();
            List<StructType> participants = new();
            foreach (var sig in functions) {
                if (sig.ReturnType is not StructType return_type) throw new NotSupportedException();

                if (!nvar.ContainsKey(return_type.Id)) {
                    nvar[return_type.Id] = 3;
                    participants.Add(return_type);
                }

                Counter<Identifier> vcounts = new();
                foreach (var arg in sig.Args) {

                    if (arg.Type is not StructType arg_type) throw new NotSupportedException();
                    var arg_type_id = arg_type.Id;

                    vcounts.Increment(arg_type_id);
                    if (!nvar.ContainsKey(arg_type_id)) {
                        nvar[arg.Type.Id] = 3;
                        participants.Add(arg_type);
                    }
                }

                foreach (var kvp in vcounts) {
                    if (nvar[kvp.Key] < kvp.Value) nvar[kvp.Key] = kvp.Value;
                }
            }

            return participants.Select(p =>
                new Clasp(
                    p,
                    Enumerable.Range(0, nvar[p.Id]).Select(i => new Variable($"{p.Name}_s{i}", p)).ToList(),
                    new Variable($"{p.Name}_alt", p)
                 )
            ).ToList();
        }
    }
}