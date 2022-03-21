using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Semgus.Util.Json {
    public class DelegateWriteOnlyConverter<T> : JsonConverter<T> {
        public delegate void WriteCallback(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

        readonly WriteCallback _callback;

        public DelegateWriteOnlyConverter(WriteCallback callback) {
            _callback = callback;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => _callback(writer, value, options);
    }
}