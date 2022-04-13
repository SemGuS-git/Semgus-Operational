#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS

using System.Text;

namespace Semgus.CommandLineInterface {
    internal class LangTuple {
        public string name;
        public List<LangPrim> elements;

        public string EqName { get; }
        public string CompareName { get; }
        public string DisjunctName { get; }

        public LangTuple(string name, List<LangPrim> elements) {
            this.name = name;
            this.elements = elements;
            EqName = "eq_" + name;
            CompareName = "compare_" + name;
            DisjunctName = "disjunct_" + name;
        }

        public void PrintAllBoilerplate(StringBuilder sb) {
            PrintStructDef(sb);
            sb.AppendLine();
            PrintEquality(sb);
            sb.AppendLine();
            PrintCompareGenerator(sb);
            sb.AppendLine();
            PrintDisjunctGenerator(sb);
            sb.AppendLine();
            PrintNonEqualityHarness(sb);
        }

        public void PrintStructDef(StringBuilder sb) {
            sb.AppendLine($"struct {name} {{");
            for (int i = 0; i < elements.Count; i++) {
                sb.AppendLine($"    {Extractor.Stringify(elements[i])} v{i};");
            }
            sb.AppendLine("}");
        }

        public void PrintEquality(StringBuilder sb) {
            sb.AppendLine($"bit {EqName}({name} a, {name} b) {{");
            sb.Append("    return ");
            for (int i = 0; i < elements.Count; i++) {
                if (i > 0) {
                    sb.Append(" && ");
                }
                sb.Append($"a.v{i} == b.v{i}");
            }
            sb.AppendLine(";");
            sb.AppendLine("}");
        }
        public void PrintCompareGenerator(StringBuilder sb) {
            sb.AppendLine($"bit {CompareName}({name} a, {name} b) {{");
            sb.AppendLine($"    bit leq = 0;");
            sb.AppendLine("    repeat(??) {");
            sb.AppendLine($"        leq = leq || {DisjunctName}(a,b);");
            sb.AppendLine("    }");
            sb.AppendLine("    return leq;");
            sb.AppendLine("}");
        }

        public void PrintDisjunctGenerator(StringBuilder sb) {
            // disjunct
            sb.AppendLine($"generator bit {DisjunctName}({name} a, {name} b) {{");
            sb.Append("    return ");
            for (int i = 0; i < elements.Count; i++) {
                if (i > 0) sb.Append(" && ");
                sb.Append(Extractor.GetAtomGeneratorName(elements[i]));
                sb.Append($"(a.v{i}, b.v{i})");
            }
            sb.AppendLine(";");
            sb.AppendLine("}");
        }

        public void PrintNonEqualityHarness(StringBuilder sb) {
            sb.Append($"harness void non_equality_");
            sb.Append(name);
            sb.AppendLine("() {");

            sb.Append($"    {name} a = new {name}(");
            sb.Append("v0=??");
            for (int i = 1; i < elements.Count; i++) { sb.Append($", v{i}=??"); }
            sb.AppendLine(");");

            sb.Append($"    {name} b = new {name}(");
            sb.Append("v0=??");
            for (int i = 1; i < elements.Count; i++) { sb.Append($", v{i}=??"); }
            sb.AppendLine(");");

            sb.AppendLine($"    assert (!{EqName}(a,b));");
            sb.AppendLine($"    assert ({CompareName}(a,b));");
            sb.AppendLine("}");
        }
        public void PrintPartialEqAssertions(StringBuilder sb, string a, string b, string c) {
            sb.AppendLine($"    // {name}: reflexivity and antisymmetry");
            sb.AppendLine($"    assert({CompareName}({a}, {b}) && {CompareName}({b}, {a})) == {EqName}({a}, {b});");
            sb.AppendLine($"    // {name}: transitivity");
            sb.AppendLine($"    assert(!{CompareName}({a}, {b}) || !{CompareName}({b}, {c}) || {CompareName}({b}, {c}));");
        }
    }
}