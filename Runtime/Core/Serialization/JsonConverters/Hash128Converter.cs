// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Hash128Converter : JsonConverter<Hash128>
    {
        public static readonly Hash128Converter Instance = new();

        private static readonly JsonEncodedText ValueProp = JsonEncodedText.Encode("value");

        private Hash128Converter() { }

        private static bool TryParseHash128(string s, out Hash128 value)
        {
            if (string.IsNullOrEmpty(s))
            {
                value = default;
                return false;
            }

            try
            {
                value = Hash128.Parse(s);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public override Hash128 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string s = reader.GetString();
                if (!TryParseHash128(s, out Hash128 h))
                {
                    throw new JsonException("Invalid Hash128 string");
                }
                return h;
            }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string s = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        if (!TryParseHash128(s, out Hash128 h))
                        {
                            throw new JsonException("Invalid or missing Hash128 value");
                        }
                        return h;
                    }
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (reader.ValueTextEquals("value"))
                        {
                            reader.Read();
                            s = reader.GetString();
                        }
                        else
                        {
                            throw new JsonException("Unknown property for Hash128");
                        }
                    }
                }
                throw new JsonException("Incomplete JSON for Hash128");
            }

            throw new JsonException($"Invalid token type {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Hash128 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
