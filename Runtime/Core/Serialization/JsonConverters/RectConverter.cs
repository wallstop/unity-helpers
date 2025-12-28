// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RectConverter : JsonConverter<Rect>
    {
        public static readonly RectConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText WProp = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText HProp = JsonEncodedText.Encode("height");

        private RectConverter() { }

        public override Rect Read(
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
            float w = 0f;
            float h = 0f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Rect(x, y, w, h);
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
                    else if (reader.ValueTextEquals("width"))
                    {
                        reader.Read();
                        w = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("height"))
                    {
                        reader.Read();
                        h = reader.GetSingle();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Rect");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Rect");
        }

        public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteNumber(WProp, value.width);
            writer.WriteNumber(HProp, value.height);
            writer.WriteEndObject();
        }
    }
}
