//using System.Text.Json;

//namespace Semgus.Interpretation {
//    public static class InterpretationGrammarExtensions {

//        private static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() {
//            WriteIndented = true,
//            Converters = {
//                    new GrammarConverter(),
//                    new ProductionConverter(),
//                    new SemanticsConverter(),
//                    new InterpretationStepConverter(),
//                    new SmtExpressionConverter(),
//                    new VariableInfoConverter(),
//            }
//        };

//        public static string ToJson(this InterpretationGrammar grammar) => JsonSerializer.Serialize(grammar, JSON_SERIALIZER_OPTIONS);
//    }
//}