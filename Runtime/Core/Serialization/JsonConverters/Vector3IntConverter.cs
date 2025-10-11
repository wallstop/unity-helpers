namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Vector3IntConverter : JsonConverter<Vector3Int>
    {
        public static readonly Vector3IntConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText ZProp = JsonEncodedText.Encode("z");

        private Vector3IntConverter() { }

        public override Vector3Int Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            int x = 0;
            int y = 0;
            int z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Vector3Int(x, y, z);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("x"))
                    {
                        reader.Read();
                        x = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("y"))
                    {
                        reader.Read();
                        y = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("z"))
                    {
                        reader.Read();
                        z = reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Vector3Int");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Vector3Int");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector3Int value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteNumber(ZProp, value.z);
            writer.WriteEndObject();
        }
    }
}
