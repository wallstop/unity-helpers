namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class KVector2Converter : JsonConverter<KVector2>
    {
        public static readonly KVector2Converter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");

        private KVector2Converter() { }

        public override KVector2 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("KVector2 must be an object with x and y");
            }

            float x = 0f;
            float y = 0f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new KVector2(x, y);
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name for KVector2");
                }
                if (reader.ValueTextEquals("x"))
                {
                    reader.Read();
                    x = reader.GetSingle();
                }
                else if (reader.ValueTextEquals("y"))
                {
                    reader.Read();
                    y = reader.GetSingle();
                }
                else
                {
                    throw new JsonException("Unknown property for KVector2");
                }
            }

            throw new JsonException("Incomplete JSON for KVector2");
        }

        public override void Write(
            Utf8JsonWriter writer,
            KVector2 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteEndObject();
        }
    }
}
