namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class BoundingSphereConverter : JsonConverter<BoundingSphere>
    {
        public static readonly BoundingSphereConverter Instance = new();

        private static readonly JsonEncodedText PositionProp = JsonEncodedText.Encode("position");
        private static readonly JsonEncodedText RadiusProp = JsonEncodedText.Encode("radius");

        private BoundingSphereConverter() { }

        public override BoundingSphere Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 p = default;
            float r = 0f;
            bool haveP = false,
                haveR = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new BoundingSphere(haveP ? p : default, haveR ? r : 0f);
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("position"))
                    {
                        reader.Read();
                        p = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveP = true;
                    }
                    else if (reader.ValueTextEquals("radius"))
                    {
                        reader.Read();
                        r = reader.GetSingle();
                        haveR = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for BoundingSphere");
                    }
                }
            }
            throw new JsonException("Incomplete JSON for BoundingSphere");
        }

        public override void Write(
            Utf8JsonWriter writer,
            BoundingSphere value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PositionProp);
            JsonSerializer.Serialize(writer, value.position, options);
            writer.WriteNumber(RadiusProp, value.radius);
            writer.WriteEndObject();
        }
    }
}
