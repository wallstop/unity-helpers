// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RectOffsetConverter : JsonConverter<RectOffset>
    {
        public static readonly RectOffsetConverter Instance = new();

        private static readonly JsonEncodedText LeftProp = JsonEncodedText.Encode("left");
        private static readonly JsonEncodedText RightProp = JsonEncodedText.Encode("right");
        private static readonly JsonEncodedText TopProp = JsonEncodedText.Encode("top");
        private static readonly JsonEncodedText BottomProp = JsonEncodedText.Encode("bottom");

        private RectOffsetConverter() { }

        public override RectOffset Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            int left = 0;
            int right = 0;
            int top = 0;
            int bottom = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new RectOffset(left, right, top, bottom);
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("left"))
                    {
                        reader.Read();
                        left = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("right"))
                    {
                        reader.Read();
                        right = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("top"))
                    {
                        reader.Read();
                        top = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("bottom"))
                    {
                        reader.Read();
                        bottom = reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for RectOffset");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for RectOffset");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RectOffset value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(LeftProp, value.left);
            writer.WriteNumber(RightProp, value.right);
            writer.WriteNumber(TopProp, value.top);
            writer.WriteNumber(BottomProp, value.bottom);
            writer.WriteEndObject();
        }
    }
}
