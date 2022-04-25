using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Helpers;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class Shared {
            public static Literal IntOffset { get; } = new(16);
            public static Literal IntMin { get; } = new (-100);
            public static Literal IntMax { get; } = new(100);

            static Assignment AdjustedAssign(Variable lhs, Variable rhs)
                => lhs.Assign(lhs.TypeId == IntType.Id ? Op.Minus.Of(rhs.Ref(), IntOffset) : rhs.Ref());

            static (IReadOnlyList<FunctionArg> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(StructType st, IReadOnlyList<Variable> input_structs) {
                List<FunctionArg> input_args = new();
                List<IStatement> input_assembly_statements = new();

                input_assembly_statements.Add(new Annotation("Assemble structs"));

                foreach (var obj in input_structs) {
                    if (obj.TypeId != st.Id) throw new ArgumentException();
                    List<FunctionArg> locals = new();
                    foreach (var prop in st.Elements) {
                        //if (prop.Type is StructType) throw new NotSupportedException();
                        locals.Add(new(new($"{obj.Id}_{prop.Id}", prop.TypeId)));
                    }

                    input_args.AddRange(locals);
                    input_assembly_statements.Add(
                        obj.Declare(
                            st.New(
                                st.Elements.Select((prop, i) => AdjustedAssign(prop, locals[i].Variable))
                            )
                        )
                    );
                }

                return (input_args, input_assembly_statements);
            }

            public static FunctionDefinition GetForallTestHarness(StructType st, FunctionDefinition test, params Variable[] vars) {
                List<IStatement> body = new();
                var (input_args, input_assembly_statements) = GetMainInitContent(st, vars);

                body.AddRange(input_assembly_statements);
                body.Add(test.Call(vars.Select(v => v.Ref()).ToList()));

                return new(new FunctionSignature(FunctionModifier.Harness, VoidType.Id, new("forall_" + test.Id.Name), input_args), body);
            }
        }
    }
}
