namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public static readonly Vector3Converter Instance = new();

        private Vector3Converter() { }

        public override Vector3 Read(
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

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Vector3(x, y, z);
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
                        default:
                        {
                            throw new JsonException($"Unknown property: {propertyName}");
                        }
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Vector3");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector3 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}
