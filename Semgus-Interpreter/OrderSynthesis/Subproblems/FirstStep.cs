using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class FirstStep {
        record Clasp(StructType Type, IReadOnlyList<Variable> Indexed, Variable Alternate);

        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<FunctionDefinition> MaybeMonotoneFunctions { get; }

        public FirstStep(IReadOnlyList<StructType> structs, IReadOnlyList<FunctionDefinition> maybeMonotoneFunctions) {
            Structs = structs;
            MaybeMonotoneFunctions = maybeMonotoneFunctions;
        }

        public static FirstStep Extract(Semgus.Operational.InterpretationGrammar grammar) {
            List<Semgus.Operational.SemanticRuleInterpreter> all_sem = new();
            List<FunctionDefinition> functions = new();
            Dictionary<string, StructType> observed = new();

            SemToSketchConverter converter = new();

            foreach (var prod in grammar.Productions.Values.SelectMany(val => val.Select(mu => mu.Production)).Distinct()) {
                if (prod.Semantics.Count != 1) throw new NotImplementedException();
                converter.RegisterProd(prod);
                all_sem.Add(prod.Semantics[0]);
            }

            foreach (var sem in all_sem) {
                var fn = converter.OpSemToFunction(new($"lang_f{functions.Count}"), sem.ProductionRule, sem.Steps);
                if (fn.Signature is not FunctionSignature sig) throw new NotSupportedException(); // Should never happen

                fn.Alias = sem.ProductionRule.ToString();

                // skip constant functions, e.g. literals
                if (sig.Args.Count == 0) continue;

                functions.Add(fn);

                observed.TryAdd(sig.ReturnType.Name, (StructType)sig.ReturnType);
                foreach (var arg in sig.Args) {
                    observed.TryAdd(arg.Type.Name, (StructType)arg.Type);
                }
            }

            return new(observed.Values.ToList(), functions);
        }

        public IEnumerable<IStatement> GetFile() {
            foreach (var st in Structs) {
                yield return st.GetStructDef();
            }
            foreach (var st in Structs) {
                yield return st.GetEqualityFunction();
                yield return st.GetCompareGenerator();
                yield return st.GetDisjunctGenerator();
            }

            yield return BitType.GetAtom();
            yield return IntType.GetAtom();

            foreach (var fn in MaybeMonotoneFunctions) {
                yield return fn;
            }

            foreach (var st in Structs) {
                yield return st.GetNonEqualityHarness();
            }

            yield return GetMain();
        }

        public FunctionDefinition GetMain() {
            var clasps = GetClasps();

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = GetMainInitContent(clasps);

            body.AddRange(input_assembly_statements);

            body.Add(new LineComment("Check partial equality properties", 2));
            foreach (var c in clasps) {
                body.AddRange(c.Type.GetPartialEqAssertions(c.Indexed[0], c.Indexed[1], c.Indexed[2]));
            }

            body.Add(new LineComment("Monotonicity", 2));

            var n_mono = new Variable("n_mono", IntType.Instance);
            body.Add(new VariableDeclaration(n_mono, new Literal(0)));

            var claspMap = clasps.ToDictionary(v => v.Type.Name);
            foreach (var fn in MaybeMonotoneFunctions) {
                body.AddRange(GetMonoAssertions(n_mono, claspMap, fn));
            }

            int n_mono_checks = MaybeMonotoneFunctions.Sum(fn => fn.Signature.Args.Count);

            body.Add(new MinimizeStatement(Op.Minus.Of(new Literal(n_mono_checks), n_mono.Ref())));

            return new FunctionDefinition(new FunctionSignature(new("main"), FunctionModifier.Harness, VoidType.Instance, input_args), body);
        }

        private static IEnumerable<IStatement> GetMonoAssertions(Variable n_mono, IReadOnlyDictionary<string, Clasp> clasps, FunctionDefinition fn) {
            if (fn.Signature is not FunctionSignature sig || sig.ReturnType is not StructType type_out) throw new NotSupportedException();

            List<VariableRef> fixed_args = new();

            {
                Counter<string> vcount = new();
                foreach (var v in sig.Args) {
                    var key = v.Type.Id.Name;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
                    vcount.Increment(key);
                }
            }

            yield return new LineComment($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < sig.Args.Count; i++) {
                if (sig.Args[i].Type is not StructType type_i) throw new NotSupportedException();

                var alt_i = clasps[type_i.Name].Alternate;

                List<VariableRef> alt_args = new(fixed_args);
                alt_args[i] = alt_i.Ref();

                var mono_flag = new Variable($"mono_{fn.Id}_{i}", IntType.Instance);

                yield return new VariableDeclaration(mono_flag, new Hole($"#MONO {fn.Alias}_{i}"));
                yield return mono_flag.IfEq(X.L0,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(fixed_args), fn.Call(alt_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), X.L1))
                );
                yield return mono_flag.ElseIfEq(X.L1,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(alt_args), fn.Call(fixed_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), X.L1))
                );
            }
        }

        private IReadOnlyList<Clasp> GetClasps() {
            Dictionary<string, int> nvar = new();
            List<StructType> participants = new();
            foreach (var fn in MaybeMonotoneFunctions) {
                if (fn.Signature is not FunctionSignature sig || sig.ReturnType is not StructType ret_st) throw new NotSupportedException();

                if (!nvar.ContainsKey(ret_st.Name)) {
                    nvar[ret_st.Name] = 3;
                    participants.Add(ret_st);
                }

                Counter<string> vcounts = new();
                foreach (var arg in sig.Args) {

                    if (arg.Type is not StructType arg_st) throw new NotSupportedException();
                    var arg_st_name = arg_st.Name;

                    vcounts.Increment(arg_st_name);
                    if (!nvar.ContainsKey(arg_st_name)) {
                        nvar[arg.Type.Name] = 3;
                        participants.Add(arg_st);
                    }
                }

                foreach (var kvp in vcounts) {
                    if (nvar[kvp.Key] < kvp.Value) nvar[kvp.Key] = kvp.Value;
                }
            }

            return participants.Select(p =>
                new Clasp(
                    p,
                    Enumerable.Range(0, nvar[p.Name]).Select(i => new Variable($"{p.Name}_s{i}", p)).ToList(),
                    new Variable($"{p.Name}_alt", p)
                 )
            ).ToList();
        }

        private static (IReadOnlyList<Variable> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<Clasp> clasps) {
            List<Variable> input_structs = clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList();

            List<Variable> input_args = new();
            List<IStatement> input_assembly_statements = new();

            input_assembly_statements.Add(new LineComment("Assemble structs"));

            foreach (var obj in input_structs) {
                if (obj.Type is not StructType st) throw new NotSupportedException();
                List<Variable> locals = new();
                foreach (var prop in st.Elements) {
                    if (prop.Type is StructType) throw new NotSupportedException();
                    locals.Add(new Variable($"{obj.Id}_{prop.Id}", prop.Type));
                }

                input_args.AddRange(locals);
                input_assembly_statements.Add(new VariableDeclaration(obj, st.New(st.Elements.Select((prop, i) => prop.Assign(locals[i].Ref())))));
            }

            return (input_args, input_assembly_statements);
        }

    }
}
