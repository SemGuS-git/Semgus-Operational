﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Semgus.Util.Json {
    public class ToStringWriteOnlyConverter<T> : JsonConverter<T> {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
            JsonSerializer.Serialize(writer, value.ToString(), options);
        }
    }
}