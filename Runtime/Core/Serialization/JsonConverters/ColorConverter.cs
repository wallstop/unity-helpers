namespace UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class ColorConverter : JsonConverter<Color>
    {
        public static readonly ColorConverter Instance = new();

        private ColorConverter() { }

        public override Color Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            float r = 0;
            float g = 0;
            float b = 0;
            float a = 1;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Color(r, g, b, a);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "r":
                        {
                            r = reader.GetSingle();
                            break;
                        }
                        case "g":
                        {
                            g = reader.GetSingle();
                            break;
                        }
                        case "b":
                        {
                            b = reader.GetSingle();
                            break;
                        }
                        case "a":
                        {
                            a = reader.GetSingle();
                            break;
                        }
                        default:
                        {
                            throw new JsonException($"Unknown property: {propertyName}");
                        }
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Color");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Color value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.r);
            writer.WriteNumber("g", value.g);
            writer.WriteNumber("b", value.b);
            writer.WriteNumber("a", value.a);
            writer.WriteEndObject();
        }
    }
}
