// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RangeIntConverter : JsonConverter<RangeInt>
    {
        public static readonly RangeIntConverter Instance = new();

        private static readonly JsonEncodedText StartProp = JsonEncodedText.Encode("start");
        private static readonly JsonEncodedText LengthProp = JsonEncodedText.Encode("length");

        private RangeIntConverter() { }

        public override RangeInt Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            int start = 0;
            int length = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new RangeInt(start, length);
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("start"))
                    {
                        reader.Read();
                        start = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("length"))
                    {
                        reader.Read();
                        length = reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for RangeInt");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for RangeInt");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RangeInt value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(StartProp, value.start);
            writer.WriteNumber(LengthProp, value.length);
            writer.WriteEndObject();
        }
    }
}
