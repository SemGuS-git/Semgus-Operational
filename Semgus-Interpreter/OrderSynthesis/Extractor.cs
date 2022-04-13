#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS

using Semgus.Model;
using Semgus.Model.Smt;
using Semgus.Operational;
using Semgus.Util;
using System.Text;

namespace Semgus.OrderSynthesis {

    internal class Extractor {
        private readonly List<LangFunction> functions = new();

        private readonly Dictionary<string, LangTuple> inputs_by_term_type = new();
        private readonly Dictionary<string, LangTuple> outputs_by_term_type = new();
        private readonly Dictionary<string, LangTuple> observed = new();

        public void Extract(InterpretationGrammar grammar) {
            List<SemanticRuleInterpreter> all_sem = new();

            foreach (var prod in grammar.Productions.Values.SelectMany(val => val.Select(mu => mu.Production)).Distinct()) {
                IncorporateInputOutputTuples(prod.TermType, prod.InputVariables, prod.OutputVariables);
                if (prod.Semantics.Count != 1) throw new NotImplementedException();
                all_sem.Add(prod.Semantics[0]);
            }

            foreach (var sem in all_sem) {
                var fn = ToFn($"lang_f{functions.Count}", sem.ProductionRule, sem.Steps);

                // skip constant functions, e.g. literals
                if (fn.Inputs.Count == 0) continue;

                functions.Add(fn);
                observed.TryAdd(fn.Sem_output.name, fn.Sem_output);
                foreach (var ii in fn.Inputs) {
                    observed.TryAdd(ii.Item2.name, ii.Item2);
                }
            }
        }

        public static SketchSyntax.

        public string PrintFile() {
            var sb = new StringBuilder();


            foreach (var kvp in observed) {
                kvp.Value.PrintAllBoilerplate(sb);
                sb.AppendLine();
                sb.AppendLine();
            }

            PrintAtoms(sb);
            sb.AppendLine();
            sb.AppendLine();

            foreach (var fn in functions) {
                fn.PrintDefinition(sb);
                sb.AppendLine();
            }

            sb.AppendLine();

            PrintMain(sb);

            return sb.ToString();
        }

        static void PrintAtoms(StringBuilder sb) {
            foreach (var at in new[] { SketchLanguage.PrimitiveType.Bit, SketchLanguage.PrimitiveType.Int }) {
                sb.Append("generator bit ");
                sb.Append(GetAtomGeneratorName(at));
                sb.Append('(');
                sb.Append(Stringify(at));
                sb.Append(" a, ");
                sb.Append(Stringify(at));
                sb.AppendLine(" b) {");
#if INT_ATOM_FLAGS
                sb.AppendLine("    int t = ??;");

                switch (at) {
                    case SketchLanguage.PrimitiveType.Bit:
                        sb.AppendLine("    if(t==0) { return (!a) || b; }");
                        sb.AppendLine("    return 1;");
                        break;
                    case SketchLanguage.PrimitiveType.Int:
                        sb.AppendLine("    if(t==0) { return a==b; }");
                        sb.AppendLine("    if(t==1) { return a<=b; }");
                        sb.AppendLine("    if(t==2) { return a< b; }");
                        sb.AppendLine("    if(t==3) { return a>=b; }");
                        sb.AppendLine("    if(t==4) { return a> b; }");
                        sb.AppendLine("    return 1;");
                        break;
                    default: throw new NotSupportedException();
                }
#else
                switch(at) {
                    case LangPrim.Bit:
                        sb.AppendLine("    if(??) { return (!a) || b; }");
                        sb.AppendLine("    return 1;");
                        break;
                    case LangPrim.Int:
                        sb.AppendLine("    if(??) { return a==b; }");
                        sb.AppendLine("    if(??) { return a<=b; }");
                        sb.AppendLine("    if(??) { return a< b; }");
                        sb.AppendLine("    if(??) { return a>=b; }");
                        sb.AppendLine("    if(??) { return a> b; }");
                        sb.AppendLine("    return 1;");
                        break;
                    default: throw new NotSupportedException();
                }
#endif
                sb.AppendLine("}");
            }
        }

        void IncorporateInputOutputTuples(SemgusTermType tt, IReadOnlyList<VariableInfo> prod_inputs, IReadOnlyList<VariableInfo> prod_outputs) {
            var key = tt.Name.Name.Symbol;
            if (inputs_by_term_type.ContainsKey(key)) return;
            inputs_by_term_type.Add(key, new($"In_{inputs_by_term_type.Count}", prod_inputs.Select(v => SketchLanguage.MapPrim(v.Sort)).ToList()));
            outputs_by_term_type.Add(key, new($"Out_{inputs_by_term_type.Count}", prod_outputs.Select(v => SketchLanguage.MapPrim(v.Sort)).ToList()));
        }

        LangTuple GetInputsOf(SemgusTermType tt) => inputs_by_term_type[tt.Name.Name.Symbol];
        LangTuple GetOutputsOf(SemgusTermType tt) => outputs_by_term_type[tt.Name.Name.Symbol];
        LangTuple GetOutputsOf(string key) => outputs_by_term_type[key];

        LangFunction ToFn(string name, ProductionRuleInterpreter prod, IReadOnlyList<IInterpretationStep> steps) {
            var sem_input = GetInputsOf(prod.TermType);
            var sem_output = GetOutputsOf(prod.TermType);

            List<(string, LangTuple)> inputs = new();

            HashSet<string> inputVarNames = new(prod.InputVariables.Select(v => v.Name));
            Dictionary<string, NextVar> labelMap = new();

            List<string> lines = new();
            int n_aux = 0;

            for (int i = 0; i < prod.InputVariables.Count; i++) {
                labelMap.Add(prod.InputVariables[i].Name, new(sem_input.elements[i], $"f_x.v{i}"));
            }
            for (int i = 0; i < prod.OutputVariables.Count; i++) {
                labelMap.Add(prod.OutputVariables[i].Name, new(sem_output.elements[i], $"r{i}"));
            }

            bool includes_sem_input = false;

            foreach (var step in steps) {
                switch (step) {
                    case ConditionalAssertion condat:
                        break;
                    case TermEvaluation termeval:
                        var tuple_name = $"f_y{inputs.Count}";

                        var local_outputs = GetOutputsOf(termeval.Term.TermTypeKey);
                        inputs.Add((tuple_name, local_outputs));

                        for (int i = 0; i < termeval.OutputVariables.Count; i++) {
                            VariableInfo ovar = termeval.OutputVariables[i];
                            labelMap.Add(ovar.Name, new(local_outputs.elements[i], $"{tuple_name}.v{i}"));
                        }
                        break;
                    case AssignmentFromLocalFormula assign:
                        if (assign.DependencyVariables.Any(v => inputVarNames.Contains(v.Name))) {
                            includes_sem_input = true;
                        }

                        var sb = new StringBuilder();

                        if (!labelMap.TryGetValue(assign.ResultVar.Name, out var result)) {
                            // I don't think this ever gets called anymore
                            var mp = SketchLanguage.MapPrim(assign.ResultVar.Sort);
                            result = new(mp, $"a{n_aux}");
                            n_aux++;
                        }

                        sb.Append(result.Decl);
                        sb.Append(" = ");
                        SketchLanguage.DoExpression(sb, labelMap, assign.Expression);
                        sb.Append(';');
                        lines.Add(sb.ToString());
                        break;
                }
            }

            if (includes_sem_input) {
                inputs.Insert(0, ("f_x", sem_input));
            }

            var return_sb = new StringBuilder();

            return_sb.Append("return new ");
            return_sb.Append(sem_output.name);
            return_sb.Append('(');
            return_sb.Append("v0=");
            return_sb.Append(labelMap[prod.OutputVariables[0].Name].Name);
            for (int i = 1; i < prod.OutputVariables.Count; i++) {
                return_sb.Append(", v");
                return_sb.Append(i);
                return_sb.Append('=');
                return_sb.Append(labelMap[prod.OutputVariables[i].Name].Name);
            }
            return_sb.Append(')');
            return_sb.Append(';');

            lines.Add(return_sb.ToString());

            return new(name, prod.ToString(), sem_output, inputs, lines);
        }



        public static string Stringify(SketchLanguage.PrimitiveType prim) => prim switch {
            SketchLanguage.PrimitiveType.Bit => "bit",
            SketchLanguage.PrimitiveType.Int => "int",
            _ => throw new NotImplementedException(),
        };

        private void PrintMain(StringBuilder sb) {
            Dictionary<string, int> nvar = new();
            List<LangTuple> participants = new();

            foreach (var fn in functions) {
                if (!nvar.ContainsKey(fn.Sem_output.name)) {
                    nvar[fn.Sem_output.name] = 3;
                    participants.Add(fn.Sem_output);
                }

                Counter<string> vcounts = new();
                foreach (var (_, a) in fn.Inputs) {
                    vcounts.Increment(a.name);
                    if (!nvar.ContainsKey(a.name)) {
                        nvar[a.name] = 3;
                        participants.Add(a);
                    }
                }

                foreach (var kvp in vcounts) {
                    if (nvar[kvp.Key] < kvp.Value) nvar[kvp.Key] = kvp.Value;
                }
            }


            List<string> lines_signature = new();
            List<string> lines_ctors = new();

            foreach (var p in participants) {
                var sbl = new StringBuilder();
                var sbc = new StringBuilder();

                var c = nvar[p.name];
                for (int i = 0; i < c; i++) {
                    var struct_i = $"{p.name}_s{i}";

                    sbl.Append("   ");
                    sbc.Append($"    {p.name} {struct_i} = new {p.name}(");
                    for (int j = 0; j < p.elements.Count; j++) {
                        sbl.Append(' ');
                        sbl.Append(Stringify(p.elements[j]));
                        sbl.Append(' ');
                        sbl.Append($"{struct_i}_v{j},");

                        if (j > 0) sbc.Append(", ");
                        sbc.Append($"v{j}={struct_i}_v{j}");
                    }
                    sbc.Append(");");
                    lines_signature.Add(sbl.ToString());
                    lines_ctors.Add(sbc.ToString());
                    sbl.Clear();
                    sbc.Clear();
                }

                sbl.Append("   ");
                sbc.Append($"    {p.name} {p.name}_alt = new {p.name}(");
                for (int j = 0; j < p.elements.Count; j++) {
                    sbl.Append(' ');
                    sbl.Append(Stringify(p.elements[j]));
                    sbl.Append(' ');
                    sbl.Append($"{p.name}_alt_v{j},");

                    if (j > 0) sbc.Append(", ");
                    sbc.Append($"v{j}={p.name}_alt_v{j}");
                }
                sbc.Append(");");
                lines_signature.Add(sbl.ToString());
                lines_ctors.Add(sbc.ToString());
            }

            var indent1 = "    ";

            sb.Append("harness void main(");
            foreach (var line in lines_signature) {
                sb.AppendLine();
                sb.Append(line);
            }
            sb.Length--; // strip trailing comma
            sb.AppendLine();
            sb.AppendLine(") {");


            sb.AppendLine("    // Struct assembly");
            foreach (var line in lines_ctors) {
                sb.AppendLine(line);
            }

            sb.AppendLine(indent1);
            sb.AppendLine("    // Partial equality properties");

            foreach (var of in inputs_by_term_type.Values.Concat(outputs_by_term_type.Values)) {
                if (!nvar.ContainsKey(of.name)) continue;
                of.PrintPartialEqAssertions(sb, $"{of.name}_s0", $"{of.name}_s1", $"{of.name}_s2");
                sb.AppendLine(indent1);
            }
            sb.AppendLine(indent1);
            sb.AppendLine("    // Monotonicity");

            sb.Append(indent1); sb.AppendLine("int n_mono = 0;");
            sb.AppendLine(indent1);
            foreach (var fn in functions) {
                fn.PrintMonotonicityAssertion(sb);
                sb.AppendLine(indent1);
            }

            int n_mono_checks = functions.Sum(p => p.Inputs.Count);

            sb.AppendLine($"    minimize({n_mono_checks}-n_mono);");
            sb.AppendLine("}");
        }
        public static string GetAtomGeneratorName(SketchLanguage.PrimitiveType langPrim) => langPrim switch {
            SketchLanguage.PrimitiveType.Bit => "atom_bit",
            SketchLanguage.PrimitiveType.Int => "atom_int",
            _ => throw new NotSupportedException(),
        };
    }
}