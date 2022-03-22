using System.Text.Json;
using System.Text.Json.Serialization;

namespace Semgus.Operational {
    public static class SerializationExtensions {

        private static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() {
            WriteIndented = true,
            Converters = {
                    new InterpretationLibraryConverter(),
                    new ProductionConverter(),
                    new SemanticsConverter(),
                    new InterpretationStepConverter(),
                    new SmtExpressionConverter(),
                    new VariableInfoConverter(),
            }
        };

        public static string ToJson(this InterpretationLibrary grammar) => JsonSerializer.Serialize(grammar, JSON_SERIALIZER_OPTIONS);
    
        //public class GrammarConverter : JsonConverter<InterpretationGrammar> {
        //    public override InterpretationGrammar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        //        throw new NotImplementedException();
        //    }

        //    public override void Write(Utf8JsonWriter writer, InterpretationGrammar value, JsonSerializerOptions options) {
        //        writer.WriteStartObject();
        //        //writer.WriteString("theory_id", value.Theory.Identifier);
        //        writer.WriteStartObject("contents");
        //        foreach (var kvp in value.Productions) {
        //            writer.WriteStartArray(kvp.Key.Name);
        //            foreach (var prod in kvp.Value) {
        //                JsonSerializer.Serialize(writer, prod, options);
        //            }
        //            writer.WriteEndArray();
        //        }
        //        writer.WriteEndObject();
        //        writer.WriteEndObject();
        //    }
        //}
        public class InterpretationLibraryConverter : JsonConverter<InterpretationLibrary> {
            public override InterpretationLibrary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, InterpretationLibrary value, JsonSerializerOptions options) {
                writer.WriteStartObject();
                //writer.WriteString("theory_id", value.Theory.Identifier);
                writer.WriteStartArray("contents");
                foreach (var prod in value.Productions) {
                    JsonSerializer.Serialize(writer, prod, options);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }


        public class ProductionConverter : JsonConverter<ProductionRuleInterpreter> {
            public override ProductionRuleInterpreter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, ProductionRuleInterpreter value, JsonSerializerOptions options) {
                writer.WriteStartObject();

                writer.WriteStartObject("syntax");

                writer.WriteString("term_type", value.TermType.Name.AsString());

                writer.WriteString("ctor", value.SyntaxConstructor.Operator.AsString());

                writer.WriteStartArray("child_term_types");
                foreach (var k in value.SyntaxConstructor.Children) {
                    writer.WriteStringValue(k.Name.AsString());
                }
                writer.WriteEndArray();

                writer.WriteEndObject();

                writer.WriteStartArray("input_var");
                foreach (var k in value.InputVariables) {
                    JsonSerializer.Serialize(writer, k, options);
                }
                writer.WriteEndArray();

                writer.WriteStartArray("output_var");
                foreach (var k in value.OutputVariables) {
                    JsonSerializer.Serialize(writer, k, options);
                }
                writer.WriteEndArray();

                writer.WriteNumber("scratch_size", value.ScratchSize);

                writer.WriteStartArray("semantics");
                foreach (var k in value.Semantics) {
                    JsonSerializer.Serialize(writer, k, options);
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

        }

        public class SemanticsConverter : JsonConverter<SemanticRuleInterpreter> {
            public override SemanticRuleInterpreter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, SemanticRuleInterpreter value, JsonSerializerOptions options) {
                writer.WriteStartObject();

                writer.WriteStartArray("instructions");
                foreach (var k in value.Steps) {
                    JsonSerializer.Serialize(writer, k, options);
                }
                writer.WriteEndArray();
                writer.WriteStartArray("auxiliary_var");
                foreach (var k in value.AuxiliaryVariables) {
                    JsonSerializer.Serialize(writer, k, options);
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        }

        public class InterpretationStepConverter : JsonConverter<IInterpretationStep> {
            public override IInterpretationStep Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, IInterpretationStep value, JsonSerializerOptions options) {
                switch (value) {
                    case ConditionalAssertion a:
                        WriteC(writer, a, options); break;
                    case AssignmentFromLocalFormula a:
                        WriteC(writer, a, options); break;
                    case TermEvaluation a:
                        WriteC(writer, a, options); break;
                    default:
                        throw new NotSupportedException();
                }
            }

            private void WriteC(Utf8JsonWriter writer, TermEvaluation a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WriteStartObject("TermEvaluation");
                writer.WriteNumber("term_index", a.Term.Index);
                // TODO: write full-fat variableinfos
                writer.WriteStartArray("in_var_index");
                foreach (var k in a.InputVariables) {
                    writer.WriteNumberValue(k.Index);
                }
                writer.WriteEndArray();
                writer.WriteStartArray("out_var_index");
                foreach (var k in a.OutputVariables) {
                    writer.WriteNumberValue(k.Index);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            private void WriteC(Utf8JsonWriter writer, AssignmentFromLocalFormula a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WriteStartArray("SimpleAssignment");
                JsonSerializer.Serialize(writer, a.Expression, options);
                JsonSerializer.Serialize(writer, a.ResultVar, options);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            private void WriteC(Utf8JsonWriter writer, ConditionalAssertion a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WritePropertyName("ConditionalAssertion");
                JsonSerializer.Serialize(writer, a.Expression, options);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
        }
        public class SmtExpressionConverter : JsonConverter<ISmtLibExpression> {
            public override ISmtLibExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, ISmtLibExpression value, JsonSerializerOptions options) {
                switch (value) {
                    case FunctionCallExpression a:
                        WriteC(writer, a, options); break;
                    case LiteralExpression a:
                        WriteC(writer, a, options); break;
                    case VariableEvalExpression a:
                        WriteC(writer, a, options); break;
                    default:
                        throw new NotSupportedException();
                }
            }

            private void WriteC(Utf8JsonWriter writer, VariableEvalExpression a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WritePropertyName("Variable");
                JsonSerializer.Serialize(writer, a.Variable, options);
                writer.WriteEndObject();
            }

            private void WriteC(Utf8JsonWriter writer, LiteralExpression a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WriteStartObject("Literal");
                writer.WriteString("kind", a.Sort.Name.AsString());
                writer.WriteString("value", a.BoxedValue.ToString());
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            private void WriteC(Utf8JsonWriter writer, FunctionCallExpression a, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WriteStartArray("Operation");
                writer.WriteStringValue(a.Function.Name);
                JsonSerializer.Serialize(writer, a.Args, options);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

        }

        public static class SerializationHelpers {
            private static readonly Dictionary<Type, string> TYPE_MAP = new() {
                { typeof(bool), "Bool" },
                { typeof(int), "Int" },
            };
            public static string GetTypeString(Type type) => TYPE_MAP[type];
        }

        public class VariableInfoConverter : JsonConverter<VariableInfo> {
            public override VariableInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, VariableInfo value, JsonSerializerOptions options) {
                writer.WriteStartObject();
                writer.WriteString("name", value.Name);
                writer.WriteNumber("index", value.Index);
                writer.WriteString("val_type", value.Sort.Name.AsString());
                writer.WriteEndObject();
            }
        }
    }

}