namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class BoundsConverter : JsonConverter<Bounds>
    {
        public static readonly BoundsConverter Instance = new();

        private static readonly JsonEncodedText CenterProp = JsonEncodedText.Encode("center");
        private static readonly JsonEncodedText SizeProp = JsonEncodedText.Encode("size");

        private BoundsConverter() { }

        public override Bounds Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 center = default;
            Vector3 size = default;
            bool haveCenter = false;
            bool haveSize = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Bounds(haveCenter ? center : default, haveSize ? size : default);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("center"))
                    {
                        reader.Read();
                        center = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveCenter = true;
                    }
                    else if (reader.ValueTextEquals("size"))
                    {
                        reader.Read();
                        size = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveSize = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Bounds");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Bounds");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Bounds value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(CenterProp);
            JsonSerializer.Serialize(writer, value.center, options);
            writer.WritePropertyName(SizeProp);
            JsonSerializer.Serialize(writer, value.size, options);
            writer.WriteEndObject();
        }
    }
}
