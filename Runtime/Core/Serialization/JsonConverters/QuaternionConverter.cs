// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public static readonly QuaternionConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText ZProp = JsonEncodedText.Encode("z");
        private static readonly JsonEncodedText WProp = JsonEncodedText.Encode("w");

        private QuaternionConverter() { }

        public override Quaternion Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            float x = 0f;
            float y = 0f;
            float z = 0f;
            float w = 1f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Quaternion(x, y, z, w);
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
                    else if (reader.ValueTextEquals("w"))
                    {
                        reader.Read();
                        w = reader.GetSingle();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Quaternion");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Quaternion");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Quaternion value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteNumber(ZProp, value.z);
            writer.WriteNumber(WProp, value.w);
            writer.WriteEndObject();
        }
    }
}
