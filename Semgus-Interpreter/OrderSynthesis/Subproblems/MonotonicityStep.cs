﻿using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.Util;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.Subproblems {
    internal class MonotonicityStep {

        public IReadOnlyList<StructType> Structs { get; }
        public IReadOnlyList<FunctionDefinition> MaybeMonotoneFunctions { get; }

        private IReadOnlyList<FunctionDefinition> NonMonotoneFunctions { get; }

        public MonotonicityStep(IReadOnlyList<StructType> structs, IReadOnlyList<FunctionDefinition> maybeMonotoneFunctions, IReadOnlyList<FunctionDefinition> nonMonotoneFunctions) {
            Structs = structs;
            MaybeMonotoneFunctions = maybeMonotoneFunctions;
            NonMonotoneFunctions = nonMonotoneFunctions;
        }

        public static MonotonicityStep FromSemgusGrammar(Operational.InterpretationGrammar grammar) {
            List<Semgus.Operational.SemanticRuleInterpreter> all_sem = new();
            List<FunctionDefinition> functions = new();
            Dictionary<Identifier, StructType> observed_struct_types = new();
            List<FunctionDefinition> non_mono = new();

            SemToSketchConverter converter = new();

            foreach (var prod in grammar.Productions.Values.SelectMany(val => val.Select(mu => mu.Production)).Distinct()) {
                if (prod.Semantics.Count != 1) throw new NotImplementedException();
                converter.RegisterProd(prod);
                all_sem.Add(prod.Semantics[0]);
            }

            foreach (var sem in all_sem) {
                var fn = converter.OpSemToFunction(new($"lang_f{functions.Count+non_mono.Count}"), sem.ProductionRule, sem.Steps);
                if (fn.Signature is not FunctionSignature sig) throw new NotSupportedException(); // Should never happen

                fn.Alias = sem.ProductionRule.ToString();

                // skip constant functions, e.g. literals
                if (sig.Args.Count == 0) {
                    non_mono.Add(fn);
                    continue;
                }

                functions.Add(fn);

                observed_struct_types.TryAdd(sig.ReturnType.Id, (StructType)sig.ReturnType);
                foreach (var arg in sig.Args) {
                    observed_struct_types.TryAdd(arg.Type.Id, (StructType)arg.Type);
                }
            }

            return new(observed_struct_types.Values.ToList(), functions, non_mono);
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
            var clasps = Clasp.GetAll(MaybeMonotoneFunctions.Select(f => f.Signature).Cast<FunctionSignature>());

            List<IStatement> body = new();

            var (input_args, input_assembly_statements) = GetMainInitContent(clasps.SelectMany(c => c.Indexed.Append(c.Alternate)).ToList());

            body.AddRange(input_assembly_statements);

            body.Add(new Annotation("Check partial equality properties", 2));
            foreach (var c in clasps) {
                body.AddRange(c.Type.GetPartialEqAssertions(c.Indexed[0], c.Indexed[1], c.Indexed[2]));
            }

            body.Add(new Annotation("Monotonicity", 2));

            var n_mono = new Variable("n_mono", IntType.Instance);
            body.Add(new VariableDeclaration(n_mono, new Literal(0)));

            var claspMap = clasps.ToDictionary(v => v.Type.Id);
            foreach (var fn in MaybeMonotoneFunctions) {
                body.AddRange(GetMonoAssertions(n_mono, claspMap, fn));
            }

            int n_mono_checks = MaybeMonotoneFunctions.Sum(fn => fn.Signature.Args.Count);

            body.Add(new MinimizeStatement(Op.Minus.Of(new Literal(n_mono_checks), n_mono.Ref())));

            return new FunctionDefinition(new FunctionSignature(new("main"), FunctionModifier.Harness, VoidType.Instance, input_args), body);
        }

        private static IEnumerable<IStatement> GetMonoAssertions(Variable n_mono, IReadOnlyDictionary<Identifier, Clasp> clasps, FunctionDefinition fn) {
            if (fn.Signature is not FunctionSignature sig || sig.ReturnType is not StructType type_out) throw new NotSupportedException();

            List<VariableRef> fixed_args = new();

            {
                Counter<Identifier> vcount = new();
                foreach (var v in sig.Args) {
                    var key = v.Type.Id;
                    fixed_args.Add(clasps[key].Indexed[vcount.Peek(key)].Ref());
                    vcount.Increment(key);
                }
            }

            yield return new Annotation($"Monotonicity of {fn.Id} ({fn.Alias})", 1);

            for (int i = 0; i < sig.Args.Count; i++) {
                if (sig.Args[i].Type is not StructType type_i) throw new NotSupportedException();

                var alt_i = clasps[type_i.Id].Alternate;

                List<VariableRef> alt_args = new(fixed_args);
                alt_args[i] = alt_i.Ref();

                var mono_flag = new Variable($"mono_{fn.Id}_{i}", IntType.Instance);

                yield return new VariableDeclaration(mono_flag, new Hole($"#MONO {fn.Id}_{i}"));
                yield return mono_flag.IfEq(X.L0,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(fixed_args), fn.Call(alt_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), X.L1))
                );
                yield return mono_flag.IfEq(X.L1,
                    new AssertStatement(
                        type_i.Compare(fixed_args[i], alt_i.Ref()).Implies(type_out.Compare(fn.Call(alt_args), fn.Call(fixed_args)))
                    ),
                    n_mono.Assign(Op.Plus.Of(n_mono.Ref(), X.L1))
                );
            }
        }

        public static (IReadOnlyList<Variable> input_args, IReadOnlyList<IStatement> input_assembly_statements) GetMainInitContent(IReadOnlyList<Variable> input_structs) {
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
                input_assembly_statements.Add(new VariableDeclaration(obj, st.New(st.Elements.Select((prop, i) => prop.Assign(locals[i].Ref())))));
            }

            return (input_args, input_assembly_statements);
        }
        public record Output(IReadOnlyList<FunctionDefinition> Comparisons, IReadOnlyList<MonotoneLabeling> MonoFunctions, IReadOnlyList<FunctionDefinition> NonMonoFunctions);

        public async Task<Output> Execute(FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);
            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            var file_mono = dir.Append("result.mono.json");
            var file_cmp = dir.Append("result.comparisons.sk");

            System.Console.WriteLine($"--- [Initial] Writing input file at {file_in} ---");
            WriteSketchInputFile(file_in);

            System.Console.WriteLine($"--- [Initial] Invoking Sketch on {file_in} ---");

            var sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (sketch_result) {
                Console.WriteLine($"--- [Initial] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Initial] Sketch rejected; halting ---");
                throw new Exception("Sketch rejected");
            }

            Console.WriteLine($"--- [Initial] Extracting monotonicities ---");

            var mono = await InspectMonotonicities(file_in, file_holes, file_mono);
            Debug.Assert(mono.Count == MaybeMonotoneFunctions.Count, "Missing monotonicity labels for some functions; halting");

            //await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_cmp.PathWsl);

            Console.WriteLine($"--- [Initial] Reading compare functions ---");

            var compare_functions = PipelineUtil.ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), this.Structs.Select(s => s.CompareId));
            Debug.Assert(compare_functions.Count == this.Structs.Count, "Failed to extract all comparison functions; halting");

            Console.WriteLine($"--- [Initial] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted = PipelineUtil.Compactify(compare_functions); // May throw

            return new(compacted, mono, NonMonotoneFunctions);
        }

        private async Task<IReadOnlyList<MonotoneLabeling>> InspectMonotonicities(FlexPath file_in, FlexPath file_holes, FlexPath file_mono) {
            await Wsl.RunPython("read-mono-from-xml.py", file_in.PathWsl, file_holes.PathWsl, file_mono.PathWsl);
            return (await MonotoneLabeling.ExtractFromJson(this.MaybeMonotoneFunctions, file_mono.PathWin)).ToList();
        }

        private void WriteSketchInputFile(FlexPath file_in) {
            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in this.GetFile()) {
                    a.WriteInto(receiver);
                }
            }
        }
    }
}
