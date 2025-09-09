namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Vector4Converter : JsonConverter<Vector4>
    {
        public static readonly Vector4Converter Instance = new();

        private Vector4Converter() { }

        public override Vector4 Read(
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
            float z = 0;
            float w = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Vector4(x, y, z, w);
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
                        case "z":
                        {
                            z = reader.GetSingle();
                            break;
                        }
                        case "w":
                        {
                            w = reader.GetSingle();
                            break;
                        }
                        default:
                        {
                            throw new JsonException($"Unknown property: {propertyName}");
                        }
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Vector4");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector4 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteNumber("w", value.w);
            writer.WriteEndObject();
        }
    }
}
