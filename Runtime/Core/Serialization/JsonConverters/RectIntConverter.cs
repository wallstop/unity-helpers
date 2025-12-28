// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RectIntConverter : JsonConverter<RectInt>
    {
        public static readonly RectIntConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText WProp = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText HProp = JsonEncodedText.Encode("height");

        private RectIntConverter() { }

        public override RectInt Read(
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
            int w = 0;
            int h = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new RectInt(x, y, w, h);
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
                    else if (reader.ValueTextEquals("width"))
                    {
                        reader.Read();
                        w = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("height"))
                    {
                        reader.Read();
                        h = reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for RectInt");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for RectInt");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RectInt value,
            JsonSerializerOptions options
        )
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
