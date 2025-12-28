// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RayConverter : JsonConverter<Ray>
    {
        public static readonly RayConverter Instance = new();

        private static readonly JsonEncodedText OriginProp = JsonEncodedText.Encode("origin");
        private static readonly JsonEncodedText DirectionProp = JsonEncodedText.Encode("direction");

        private RayConverter() { }

        public override Ray Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 origin = default;
            Vector3 direction = Vector3.forward;
            bool haveOrigin = false;
            bool haveDirection = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Ray(
                        haveOrigin ? origin : default,
                        haveDirection ? direction : Vector3.forward
                    );
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("origin"))
                    {
                        reader.Read();
                        origin = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveOrigin = true;
                    }
                    else if (reader.ValueTextEquals("direction"))
                    {
                        reader.Read();
                        direction = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveDirection = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Ray");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Ray");
        }

        public override void Write(Utf8JsonWriter writer, Ray value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(OriginProp);
            JsonSerializer.Serialize(writer, value.origin, options);
            writer.WritePropertyName(DirectionProp);
            JsonSerializer.Serialize(writer, value.direction, options);
            writer.WriteEndObject();
        }
    }
}
