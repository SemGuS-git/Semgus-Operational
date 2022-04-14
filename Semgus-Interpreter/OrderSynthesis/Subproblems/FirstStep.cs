using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.Util;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class FirstStep {
        record Clasp(StructType Type, IReadOnlyList<VarId> Indexed, VarId Alternate);

        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<FunctionDefinition> MaybeMonotoneFunctions { get; }

        public FirstStep(IReadOnlyList<StructType> structs, IReadOnlyList<FunctionDefinition> maybeMonotoneFunctions) {
            Structs = structs;
            MaybeMonotoneFunctions = maybeMonotoneFunctions;
        }

        public static FirstStep Extract(InterpretationGrammar grammar) {
            List<SemanticRuleInterpreter> all_sem = new();
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
                fn.Alias = sem.ProductionRule.ToString();
                // skip constant functions, e.g. literals
                if (fn.Args.Count == 0) continue;

                functions.Add(fn);

                observed.TryAdd(fn.ReturnType.Name, (StructType)fn.ReturnType);
                foreach (var arg in fn.Args) {
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

            var n_mono = new VarId("n_mono", IntType.Instance);
            body.Add(new VarDeclare(n_mono, new Literal(0)));

            var claspMap = clasps.ToDictionary(v => v.Type.Name);
            foreach (var fn in MaybeMonotoneFunctions) {
                body.AddRange(GetMonoAssertions(n_mono, claspMap, fn));
            }

            int n_mono_checks = MaybeMonotoneFunctions.Sum(fn => fn.Args.Count);

            body.Add(new MinimizeStatement(Op.Minus.Of(new Literal(n_mono_checks), n_mono)));

            return new FunctionDefinition(new("main"), FunctionFlag.Harness, VoidType.Instance, input_args, body);
        }

        private static IEnumerable<IStatement> GetMonoAssertions(VarId n_mono, IReadOnlyDictionary<string, Clasp> clasps, FunctionDefinition fn) {
            if (fn.ReturnType is not StructType type_out) throw new NotSupportedException();

            List<VarId> fixed_args = new();

            {
                Counter<string> vcount = new();
                foreach (var v in fn.Args) {
                    var key = v.Type.Name;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)]);
                    vcount.Increment(key);
                }
            }

            yield return new LineComment($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < fn.Args.Count; i++) {
                if (fn.Args[i].Type is not StructType type_i) throw new NotSupportedException();

                var alt_i = clasps[type_i.Name].Alternate;

                List<VarId> alt_args = new(fixed_args);
                alt_args[i] = alt_i;

                var mono_flag = new VarId($"mono_{fn.Id}_{i}", IntType.Instance);

                yield return new VarDeclare(mono_flag, new Hole($"#MONO {fn.Alias}_{i}"));
                yield return mono_flag.IfEq(X.L0,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i).Implies(type_out.Compare(fn.Call(fixed_args), fn.Call(alt_args)))
                    ),
                    n_mono.Set(Op.Plus.Of(n_mono, X.L1))
                );
                yield return mono_flag.ElseIfEq(X.L1,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i).Implies(type_out.Compare(fn.Call(alt_args), fn.Call(fixed_args)))
                    ),
                    n_mono.Set(Op.Plus.Of(n_mono, X.L1))
                );
            }
        }

        private IReadOnlyList<Clasp> GetClasps() {
            Dictionary<string, int> nvar = new();
            List<StructType> participants = new();
            foreach (var fn in MaybeMonotoneFunctions) {
                if (fn.ReturnType is not StructType ret_st) throw new NotSupportedException();

                if (!nvar.ContainsKey(ret_st.Name)) {
                    nvar[ret_st.Name] = 3;
                    participants.Add(ret_st);
                }

                Counter<string> vcounts = new();
                foreach (var arg in fn.Args) {

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
                    Enumerable.Range(0, nvar[p.Name]).Select(i => new VarId($"{p.Name}_s{i}", p)).ToList(),
                    new VarId($"{p.Name}_alt", p)
                 )
            ).ToList();
        }

        private static (IReadOnlyList<VarId> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<Clasp> clasps) {
            List<VarId> input_structs = clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList();

            List<VarId> input_args = new();
            List<IStatement> input_assembly_statements = new();

            input_assembly_statements.Add(new LineComment("Assemble structs"));

            foreach (var obj in input_structs) {
                if (obj.Type is not StructType st) throw new NotSupportedException();
                List<VarId> locals = new();
                foreach (var prop in st.Elements) {
                    if (prop.Type is StructType) throw new NotSupportedException();
                    locals.Add(new VarId($"{obj.Name}_{prop.Name}", prop.Type));
                }

                input_args.AddRange(locals);
                input_assembly_statements.Add(new VarDeclare(obj, new NewExpression(st, st.Elements.Select((prop, i) => new Assignment(prop, locals[i])).ToList())));
            }

            return (input_args, input_assembly_statements);
        }

    }
}
