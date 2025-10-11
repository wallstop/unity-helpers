namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class BoundsIntConverter : JsonConverter<BoundsInt>
    {
        public static readonly BoundsIntConverter Instance = new();

        private static readonly JsonEncodedText PositionProp = JsonEncodedText.Encode("position");
        private static readonly JsonEncodedText SizeProp = JsonEncodedText.Encode("size");

        private BoundsIntConverter() { }

        public override BoundsInt Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3Int position = default;
            Vector3Int size = default;
            bool havePosition = false;
            bool haveSize = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new BoundsInt(
                        havePosition ? position : default,
                        haveSize ? size : default
                    );
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("position"))
                    {
                        reader.Read();
                        position = JsonSerializer.Deserialize<Vector3Int>(ref reader, options);
                        havePosition = true;
                    }
                    else if (reader.ValueTextEquals("size"))
                    {
                        reader.Read();
                        size = JsonSerializer.Deserialize<Vector3Int>(ref reader, options);
                        haveSize = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for BoundsInt");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for BoundsInt");
        }

        public override void Write(
            Utf8JsonWriter writer,
            BoundsInt value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PositionProp);
            JsonSerializer.Serialize(writer, value.position, options);
            writer.WritePropertyName(SizeProp);
            JsonSerializer.Serialize(writer, value.size, options);
            writer.WriteEndObject();
        }
    }
}
