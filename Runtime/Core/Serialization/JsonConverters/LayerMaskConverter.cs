namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class LayerMaskConverter : JsonConverter<LayerMask>
    {
        public static readonly LayerMaskConverter Instance = new();

        private static readonly JsonEncodedText ValueProp = JsonEncodedText.Encode("value");
        private static readonly JsonEncodedText LayersProp = JsonEncodedText.Encode("layers");

        private LayerMaskConverter() { }

        public override LayerMask Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int v = reader.GetInt32();
                return (LayerMask)v;
            }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                int value = 0;
                bool hasValue = false;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return (LayerMask)value;
                    }
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (reader.ValueTextEquals("value"))
                        {
                            reader.Read();
                            value = reader.GetInt32();
                            hasValue = true;
                        }
                        else if (reader.ValueTextEquals("layers"))
                        {
                            reader.Read();
                            if (reader.TokenType != JsonTokenType.StartArray)
                            {
                                throw new JsonException("layers must be an array of strings");
                            }
                            int mask = 0;
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndArray)
                                {
                                    break;
                                }
                                string name = reader.GetString();
                                int layer = LayerMask.NameToLayer(name);
                                if (layer < 0)
                                {
                                    throw new JsonException($"Unknown layer '{name}'");
                                }
                                mask |= 1 << layer;
                            }
                            value = hasValue ? value : mask;
                        }
                        else
                        {
                            throw new JsonException("Unknown property for LayerMask");
                        }
                    }
                }

                throw new JsonException("Incomplete JSON for LayerMask");
            }

            throw new JsonException($"Invalid token type {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            LayerMask value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(ValueProp, value.value);
            writer.WriteEndObject();
        }
    }
}
