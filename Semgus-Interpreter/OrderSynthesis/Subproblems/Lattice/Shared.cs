using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;

namespace Semgus.OrderSynthesis.Subproblems {
    namespace LatticeSubstep {
        internal class Shared {
            public const int INT_MAX = 100;
            public const int INT_OFFSET = 16;


            static Assignment AdjustedAssign(Variable lhs, Variable rhs)
                => lhs.Assign(lhs.TypeId == IntType.Id ? Op.Minus.Of(rhs.Ref(), new Literal(INT_OFFSET)) : rhs.Ref());

            static (IReadOnlyList<Variable> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<Variable> input_structs) {
                List<Variable> input_args = new();
                List<IStatement> input_assembly_statements = new();

                input_assembly_statements.Add(new Annotation("Assemble structs"));

                foreach (var obj in input_structs) {
                    if (obj.Type is not StructType st) throw new NotSupportedException();
                    List<Variable> locals = new();
                    foreach (var prop in st.Elements) {
                        if (prop.Type is StructType) throw new NotSupportedException();
                        locals.Add(new Variable($"{obj.Id}_{prop.Id}", prop.Type));
                    }

                    input_args.AddRange(locals);
                    input_assembly_statements.Add(new VariableDeclaration(obj, st.New(st.Elements.Select((prop, i) => AdjustedAssign(prop, locals[i])))));
                }

                return (input_args, input_assembly_statements);
            }

            public static FunctionDefinition GetForallTestHarness(FunctionDefinition test, params Variable[] vars) {
                List<IStatement> body = new();
                var (input_args, input_assembly_statements) = GetMainInitContent(vars);

                body.AddRange(input_assembly_statements);
                body.Add(test.Call(vars.Select(v => v.Ref()).ToList()));

                return new(new FunctionSignature(FunctionModifier.Harness, VoidType.Instance, new("forall_" + test.Id.Name), input_args), body);
            }

            //public static FunctionDefinition GetRefinementMain(FunctionDefinition test, params Variable[] vars) {
            //    List<IStatement> body = new();

            //    var (input_args, input_assembly_statements) = GetMainInitContent(vars);

            //    body.AddRange(input_args.Select(var =>
            //        var.Declare(new Hole())
            //    ));

            //    body.AddRange(input_assembly_statements);
            //    body.Add(test.Call(vars.Select(v => v.Ref()).ToList()));

            //    return new(new FunctionSignature(FunctionModifier.Harness, VoidType.Instance, new("main_" + test.Id.Name), Array.Empty<Variable>()), body);
            //}
        }
    }
}
