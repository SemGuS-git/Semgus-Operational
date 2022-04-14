using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using System.Text;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class SemToSketchConverter {
        private readonly Dictionary<string, (StructType input, StructType output)> structs_by_term_type = new();

        public void RegisterProd(ProductionRuleInterpreter prod) {
            var termType = prod.TermType;
            var inputs = prod.InputVariables;
            var outputs = prod.OutputVariables;
            var key = termType.Name.Name.Symbol;

            if(structs_by_term_type.TryGetValue(key,out var st)) {

            } else {
                var n = structs_by_term_type.Count;
                StructType st_input = new(new($"In_{n}"), inputs.Select(VarToProp).ToList()) { Comment = $"{termType.Name} inputs: ({SmtArgListString(inputs)})" };
                StructType st_output = new(new($"Out_{n}"), outputs.Select(VarToProp).ToList()) { Comment = $"{termType.Name} outputs: ({SmtArgListString(outputs)})" };
                structs_by_term_type.Add(key, (st_input, st_output));
            }
        }

        private string SmtArgListString(IEnumerable<VariableInfo> args) => string.Join(" ", args.Select(a => $"({a.Sort.Name} {a.Name})"));

        private static VarId VarToProp(VariableInfo sem_var, int i) => new($"v{i}", MapSortToPrimType(sem_var.Sort));

        private static IType MapSortToPrimType(SmtSort sort) {
            if (sort.Name == SmtCommonIdentifiers.BoolSortId) return BitType.Instance;
            if (sort.Name == SmtCommonIdentifiers.IntSortId) return IntType.Instance;
            throw new NotSupportedException();
        }

        private (StructType,StructType) GetIOStructs(SemgusTermType termType) => structs_by_term_type[termType.Name.Name.Symbol];
        private (StructType,StructType) GetIOStructs(string termTypeKey) => structs_by_term_type[termTypeKey];

        public FunctionDefinition OpSemToFunction(FunctionId id, ProductionRuleInterpreter prod, IReadOnlyList<IInterpretationStep> steps) {
            var (sem_input, sem_output) = GetIOStructs(prod.TermType);

            HashSet<string> inputVarNames = new(prod.InputVariables.Select(v => v.Name));

            int n_aux = 0;

            VarId f_input_tuple = new("x", sem_input);
            FunctionNamespace nspace = new();

            for (int i = 0; i < prod.InputVariables.Count; i++) {
                var f_input_i = f_input_tuple.Prop(sem_input.Elements[i]);
                nspace.VarMap.Add(prod.InputVariables[i].Name, f_input_i);
            }

            //List<Assignment> f_output_el_setters = new();

            //for (int i = 0; i < prod.OutputVariables.Count; i++) {
            //    VarId var_out_i = new($"r{i}", sem_output.Elements[i].Type);
            //    f_output_el_setters.Add(sem_output.Elements[i].Set(var_out_i));
            //    nspace.VarMap.Add(prod.OutputVariables[i].Name, var_out_i);
            //}

            bool f_input_includes_sem_input = false;
            List<VarId> f_child_output_tuples = new();

            List<IStatement> statements = new();


            foreach (var step in steps) {
                switch (step) {
                    case ConditionalAssertion condat:
                        // TODO: support branching via if statements
                        break;

                    case TermEvaluation termeval:
                        // Create new function argument to hold the output of this child term eval
                        var (_, st_child_out) = GetIOStructs(termeval.Term.TermTypeKey);

                        VarId var_output_tuple = new($"y{f_child_output_tuples.Count}", st_child_out);

                        f_child_output_tuples.Add(var_output_tuple);

                        for(int i = 0; i < termeval.OutputVariables.Count; i++) {
                            // Map CHC variables in this eval's output slots to properties of the new function argument
                            nspace.VarMap.Add(termeval.OutputVariables[i].Name, var_output_tuple.Prop(st_child_out.Elements[i]));                 
                        }

                        break;


                    case AssignmentFromLocalFormula assign:
                        f_input_includes_sem_input |= assign.DependencyVariables.Any(v => inputVarNames.Contains(v.Name));

                        var rhs = nspace.Convert(assign.Expression);

                        if (nspace.VarMap.TryGetValue(assign.ResultVar.Name, out var subject)) {
                            statements.Add(subject.Set(rhs));
                        } else {
                            // Create new aux variable
                            VarId var_aux = new($"aux_{n_aux++}", MapSortToPrimType(assign.ResultVar.Sort));
                            nspace.VarMap.Add(assign.ResultVar.Name, var_aux);
                            statements.Add(var_aux.Declare(rhs));
                        }
                        break;
                }
            }

            statements.Add(new ReturnStatement(new NewExpression(sem_output, prod.OutputVariables.Select((v,i) => sem_output.Elements[i].Set(nspace.VarMap[v.Name])).ToList())));

            return new FunctionDefinition(id, FunctionFlag.None, sem_output, f_input_includes_sem_input ? f_child_output_tuples.Prepend(f_input_tuple).ToList() : f_child_output_tuples, statements);
        }

    }
}