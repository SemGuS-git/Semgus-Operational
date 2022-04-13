#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS

using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.Util;
using System.Text;

namespace Semgus.OrderSynthesis {
    internal class LangFunction {
        public LangFunction(string name, string raw_name, LangTuple sem_output, List<(string, LangTuple)> inputs, List<string> lines) {
            Name = name;
            this.raw_name = raw_name;
            Sem_output = sem_output;
            Inputs = inputs;
            Lines = lines;
        }

        public string Name { get; }
        public LangTuple Sem_output { get; }
        public List<(string, LangTuple)> Inputs { get; }
        public List<string> Lines { get; }

        readonly string raw_name;


        public void PrintDefinition(StringBuilder sb) {
            sb.AppendLine($"// {raw_name}");
            // signature
            sb.Append(Sem_output.name);
            sb.Append(' ');
            sb.Append(Name);
            sb.Append('(');

            for (int i = 0; i < Inputs.Count; i++) {
                if (i > 0) {
                    sb.Append(", ");
                }
                sb.Append(Inputs[i].Item2.name);
                sb.Append(' ');
                sb.Append(Inputs[i].Item1);
            }
            sb.AppendLine(") {");

            // body
            foreach (var line in Lines) {
                sb.Append("    ");
                sb.AppendLine(line);
            }
            sb.Append('}');
            sb.AppendLine();
        }

        public void PrintMonotonicityAssertion(StringBuilder sb) {
            Counter<string> vcount = new();
            List<string> ph = new();

            foreach (var v in Inputs) {
                var key = v.Item2.name;
                ph.Add($"{key}_s{vcount.Peek(key)}");
                vcount.Increment(key);
            }

            var reg_var_block = string.Join(", ", ph);

            sb.AppendLine($"    // Monotonicity of {Name} ({raw_name})");

#if SAME_DIRECTION
            sb.AppendLine($"    if(??) {{");
            sb.AppendLine($"        if(??) {{");
            // mono increasing

            for (int i = 0; i < Inputs.Count; i++) {
                var v = Inputs[i].Item2;
                sb.Append($"            assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}({reg_var_block}), {Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine(")));");
            }
            sb.AppendLine("        } else {");
            // mono decreasing

            for (int i = 0; i < Inputs.Count; i++) {
                var v = Inputs[i].Item2;
                sb.Append($"            assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine($"), {Name}({reg_var_block})));");
            }

            sb.AppendLine("        }");
            sb.AppendLine("        n_mono = n_mono + 1;");
            sb.AppendLine("    }");
#else

            for (int i = 0; i < Inputs.Count; i++) {
                var v = Inputs[i].Item2;
#if INT_MONO_FLAGS
                var mono_flag = $"mono_{Name}_{i}";
                sb.AppendLine($"    int {mono_flag} = ??; //#MONO {raw_name}_{i}");
                sb.AppendLine($"    if({mono_flag}==0) {{       // Argument {i} increasing");
                // mono increasing

                sb.Append($"        assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}({reg_var_block}), {Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine(")));");
                sb.AppendLine("        n_mono = n_mono + 1;");
                sb.AppendLine($"    }} else if({mono_flag}==1) {{   // Argument {i} decreasing");

                // mono decreasing
                sb.Append($"        assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine($"), {Name}({reg_var_block})));");
                
                sb.AppendLine("        n_mono = n_mono + 1;");
                sb.AppendLine("    }");
#else
                sb.AppendLine($"    if(??) {{       // Argument {i}");

                sb.AppendLine($"        if(??) {{   // Increasing");
                // mono increasing

                sb.Append($"            assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}({reg_var_block}), {Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine(")));");

                sb.AppendLine("        } else {   // Decreasing");

                // mono decreasing
                sb.Append($"            assert (!{v.CompareName}({ph[i]}, {v.name}_alt) || {Sem_output.CompareName}({Name}(");
                for (int j = 0; j < i; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                if (i > 0) sb.Append(", ");
                sb.Append($"{v.name}_alt");

                for (int j = i + 1; j < Inputs.Count; j++) {
                    if (j > 0) sb.Append(", ");
                    sb.Append(ph[j]);
                }
                sb.AppendLine($"), {Name}({reg_var_block})));");

                sb.AppendLine("        }");
                sb.AppendLine("        n_mono = n_mono + 1;");
                sb.AppendLine("    }");
#endif
            }
#endif

            }
    }
}