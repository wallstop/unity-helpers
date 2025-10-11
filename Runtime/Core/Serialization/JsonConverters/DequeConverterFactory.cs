namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class DequeConverterFactory : JsonConverterFactory
    {
        public static readonly DequeConverterFactory Instance = new();

        private DequeConverterFactory() { }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                && typeToConvert.GetGenericTypeDefinition() == typeof(Deque<>);
        }

        public override JsonConverter CreateConverter(
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            Type elementType = typeToConvert.GetGenericArguments()[0];
            Type convType = typeof(DequeConverter<>).MakeGenericType(elementType);
            return (JsonConverter)Activator.CreateInstance(convType);
        }

        private sealed class DequeConverter<T> : JsonConverter<Deque<T>>
        {
            public override Deque<T> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            )
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("Deque<T> expects a JSON array");
                }
                List<T> items = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                return items == null ? new Deque<T>(Deque<T>.DefaultCapacity) : new Deque<T>(items);
            }

            public override void Write(
                Utf8JsonWriter writer,
                Deque<T> value,
                JsonSerializerOptions options
            )
            {
                writer.WriteStartArray();
                foreach (T item in value)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }
                writer.WriteEndArray();
            }
        }
    }
}
