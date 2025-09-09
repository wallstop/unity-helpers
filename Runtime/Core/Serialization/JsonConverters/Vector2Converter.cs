namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Vector2Converter : JsonConverter<Vector2>
    {
        public static readonly Vector2Converter Instance = new();

        private Vector2Converter() { }

        public override Vector2 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            float x = 0;
            float y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Vector2(x, y);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "x":
                        {
                            x = reader.GetSingle();
                            break;
                        }
                        case "y":
                        {
                            y = reader.GetSingle();
                            break;
                        }
                        default:
                        {
                            throw new JsonException($"Unknown property: {propertyName}");
                        }
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Vector2");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector2 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteEndObject();
        }
    }
}
