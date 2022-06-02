using Semgus.MiniParser;
using Semgus.OrderSynthesis.AbstractInterpretation;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;
using Semgus.Util;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class MonotonicityStepBuilder {

        AbstractPreInit preInit;
        IReadOnlyDictionary<Identifier, StructType> struct_type_dict;

        public MonotonicityStepBuilder(AbstractPreInit preInit) {
            this.preInit = preInit;
            this.struct_type_dict = preInit.UniqueTupleTypes;
        }


        (StructNew, IReadOnlyList<FunctionArg>) GetConcreteTransformerBody(Operational.ProductionRuleInterpreter prod, LinearTermSubtreeAbstraction ltsa, ConcreteTransformerCore ctc) {
            var ttkey = prod.TermType.Name.Name.Symbol;

            var (sem_input, sem_output) = preInit.GetIOStructs(prod.TermType);

            HashSet<string> inputVarNames = new(prod.InputVariables.Select(v => v.Name));
            List<FunctionArg> fargs = new();

            FunctionArg f_input_tuple = new(new("x", sem_input.Id));
            FunctionNamespace nspace = new();

            for (int i = 0; i < prod.InputVariables.Count; i++) {
                var f_input_i = f_input_tuple.Variable.Get(sem_input.Elements[i]);
                nspace.VarMap.Add(prod.InputVariables[i].Name, f_input_i);
            }

            // TODO: fix or remove check
            if (true || ctc.RequiredTupleIndices.Contains(0)) fargs.Add(f_input_tuple);

            foreach (AbstractTermEvalStep v in ltsa.Steps) {
                var output_idx = v.OutputTupleIndex;
                // TODO: fix or remove check
                //if (!ctc.RequiredTupleIndices.Contains(output_idx)) continue;
                var term_idx = v.NodeTermIndex;

                var (_, target_out) = preInit.GetIOStructs(v.src.Term);

                Variable var_output_tuple = new($"y{output_idx - 1}", target_out.Id);
                fargs.Add(new(var_output_tuple));

                for (int i = 0; i < v.src.OutputVariables.Count; i++) {
                    // Map CHC variables in this eval's output slots to properties of the new function argument
                    nspace.VarMap.Add(v.src.OutputVariables[i].Name, var_output_tuple.Get(target_out.Elements[i]));
                }
            }

            var ret = sem_output.New(
                prod.OutputVariables.Select(
                    (v, i) => sem_output.Elements[i].Assign(nspace.Convert(ctc.OutputVarExpressions[i]))
                )
            );

            return (ret, fargs);
        }

        public MonotonicityStep Build() {
            var info = preInit.List;

            HashSet<Identifier> observed_struct_types = new();
            List<FunctionDefinition> queryTransformers = new();
            List<MonotoneLabeling> constantTransformers = new();
            List<Identifier> fnIds = new();

            for (int i = 0; i < info.Count; i++) {
                Identifier id = new($"prod{i}_sem");
                fnIds.Add(id);
                
                var (prod, ltsa, ctc) = info[i];

                var (ret, fargs) = GetConcreteTransformerBody(prod, ltsa, ctc);
                FunctionDefinition fn = new(new(FunctionModifier.None, ret.TypeId, id, fargs), new ReturnStatement(ret));
                fn.Alias = prod.ToString();

                var sig = fn.Signature;

                if (sig.Args.Count == 0) {
                    constantTransformers.Add(MonotoneLabeling.ZeroArgument(fn));
                } else {
                    queryTransformers.Add(fn);

                    observed_struct_types.Add(sig.ReturnTypeId);
                    foreach (var arg in sig.Args) {
                        observed_struct_types.Add(arg.TypeId);
                    }
                }
            }

            Debug.Assert(observed_struct_types.Count == struct_type_dict.Count);

            return new(struct_type_dict.Values.ToList(), queryTransformers, Array.Empty<FunctionDefinition>(), constantTransformers, fnIds);
        }
    }
}
