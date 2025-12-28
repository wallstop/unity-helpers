// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public static readonly Vector3Converter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText ZProp = JsonEncodedText.Encode("z");

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
                    else if (reader.ValueTextEquals("z"))
                    {
                        reader.Read();
                        z = reader.GetSingle();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Vector3");
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
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteNumber(ZProp, value.z);
            writer.WriteEndObject();
        }
    }
}
