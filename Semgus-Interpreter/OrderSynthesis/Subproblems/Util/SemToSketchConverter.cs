using Semgus.MiniParser;
using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using System.Text;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class SemToSketchConverter {
        private readonly Dictionary<string, (StructType input, StructType output)> structs_by_term_type = new();
        private readonly Dictionary<Identifier, StructType> all_structs = new();

        public StructType GetStructType(Identifier identifier) => all_structs[identifier];

        public void EncompassStructTypes(ProductionRuleInterpreter prod) {
            var termType = prod.TermType;
            var inputs = prod.InputVariables;
            var outputs = prod.OutputVariables;
            var key = termType.Name.Name.Symbol;

            if (!structs_by_term_type.ContainsKey(key)) {
                var n = structs_by_term_type.Count;
                StructType st_input = new(new($"In_{n}"), inputs.Select(VarToProp).ToList()) { Comment = $"{termType.Name} inputs: ({SmtArgListString(inputs)})" };
                StructType st_output = new(new($"Out_{n}"), outputs.Select(VarToProp).ToList()) { Comment = $"{termType.Name} outputs: ({SmtArgListString(outputs)})" };
                structs_by_term_type.Add(key, (st_input, st_output));
                all_structs.Add(st_input.Id, st_input);
                all_structs.Add(st_output.Id, st_output);
            }
        }

        private string SmtArgListString(IEnumerable<VariableInfo> args) => string.Join(" ", args.Select(a => $"({a.Sort.Name} {a.Name})"));

        private static Variable VarToProp(VariableInfo sem_var, int i) => new($"v{i}", MapSortToPrimTypeId(sem_var.Sort));

        private static Identifier MapSortToPrimTypeId(SmtSort sort) {
            if (sort.Name == SmtCommonIdentifiers.BoolSortId) return BitType.Id;
            if (sort.Name == SmtCommonIdentifiers.IntSortId) return IntType.Id;
            throw new NotSupportedException();
        }

        private (StructType, StructType) GetIOStructs(SemgusTermType termType) => structs_by_term_type[termType.Name.Name.Symbol];
        private (StructType, StructType) GetIOStructs(string termTypeKey) => structs_by_term_type[termTypeKey];

        public FunctionDefinition OpSemToFunction(Identifier id, ProductionRuleInterpreter prod, IReadOnlyList<IInterpretationStep> steps) {
            var (sem_input, sem_output) = GetIOStructs(prod.TermType);

            HashSet<string> inputVarNames = new(prod.InputVariables.Select(v => v.Name));

            int n_aux = 0;

            FunctionArg f_input_tuple = new(new("x", sem_input.Id));
            FunctionNamespace nspace = new();

            for (int i = 0; i < prod.InputVariables.Count; i++) {
                var f_input_i = f_input_tuple.Variable.Get(sem_input.Elements[i]);
                nspace.VarMap.Add(prod.InputVariables[i].Name, f_input_i);
            }

            //List<Assignment> f_output_el_setters = new();

            //for (int i = 0; i < prod.OutputVariables.Count; i++) {
            //    VarId var_out_i = new($"r{i}", sem_output.Elements[i].Type);
            //    f_output_el_setters.Add(sem_output.Elements[i].Set(var_out_i));
            //    nspace.VarMap.Add(prod.OutputVariables[i].Name, var_out_i);
            //}

            bool f_input_includes_sem_input = false;
            List<FunctionArg> f_child_output_tuples = new();


            List<IStatement> statements = new();


            foreach (var step in steps) {
                switch (step) {
                    case ConditionalAssertion condat:
                        // TODO: support branching via if statements
                        break;

                    case TermEvaluation termeval:
                        // Create new function argument to hold the output of this child term eval
                        var (_, st_child_out) = GetIOStructs(termeval.Term.TermTypeKey);

                        Variable var_output_tuple = new($"y{f_child_output_tuples.Count}", st_child_out.Id);

                        f_child_output_tuples.Add(new(var_output_tuple));

                        for (int i = 0; i < termeval.OutputVariables.Count; i++) {
                            // Map CHC variables in this eval's output slots to properties of the new function argument
                            nspace.VarMap.Add(termeval.OutputVariables[i].Name, var_output_tuple.Get(st_child_out.Elements[i]));
                        }

                        break;


                    case AssignmentFromLocalFormula assign:
                        f_input_includes_sem_input |= assign.DependencyVariables.Any(v => inputVarNames.Contains(v.Name));

                        var rhs = nspace.Convert(assign.Expression);

                        if (nspace.VarMap.TryGetValue(assign.ResultVar.Name, out var subject)) {
                            statements.Add(new Assignment(subject, rhs));
                        } else {
                            // Create new aux variable
                            Variable var_aux = new($"aux_{n_aux++}", MapSortToPrimTypeId(assign.ResultVar.Sort));
                            nspace.VarMap.Add(assign.ResultVar.Name, var_aux.Ref());
                            statements.Add(var_aux.Declare(rhs));
                        }
                        break;
                }
            }

            statements.Add(new ReturnStatement(sem_output.New(prod.OutputVariables.Select((v, i) => sem_output.Elements[i].Assign(nspace.VarMap[v.Name])))));

            if (f_input_includes_sem_input) f_child_output_tuples.Insert(0, f_input_tuple);

            return new FunctionDefinition(
                new FunctionSignature(
                    FunctionModifier.None,
                    sem_output.Id,
                    id,
                    f_child_output_tuples
                ),
                statements
            );
        }

    }
}