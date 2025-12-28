// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class ColorConverter : JsonConverter<Color>
    {
        public static readonly ColorConverter Instance = new();

        private static readonly JsonEncodedText RProp = JsonEncodedText.Encode("r");
        private static readonly JsonEncodedText GProp = JsonEncodedText.Encode("g");
        private static readonly JsonEncodedText BProp = JsonEncodedText.Encode("b");
        private static readonly JsonEncodedText AProp = JsonEncodedText.Encode("a");

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
                    if (reader.ValueTextEquals("r"))
                    {
                        reader.Read();
                        r = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("g"))
                    {
                        reader.Read();
                        g = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("b"))
                    {
                        reader.Read();
                        b = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("a"))
                    {
                        reader.Read();
                        a = reader.GetSingle();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Color");
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
            writer.WriteNumber(RProp, value.r);
            writer.WriteNumber(GProp, value.g);
            writer.WriteNumber(BProp, value.b);
            writer.WriteNumber(AProp, value.a);
            writer.WriteEndObject();
        }
    }
}
