// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Ray2DConverter : JsonConverter<Ray2D>
    {
        public static readonly Ray2DConverter Instance = new();

        private static readonly JsonEncodedText OriginProp = JsonEncodedText.Encode("origin");
        private static readonly JsonEncodedText DirectionProp = JsonEncodedText.Encode("direction");

        private Ray2DConverter() { }

        public override Ray2D Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector2 origin = default;
            Vector2 direction = Vector2.right;
            bool haveOrigin = false;
            bool haveDirection = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Ray2D(
                        haveOrigin ? origin : default,
                        haveDirection ? direction : Vector2.right
                    );
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("origin"))
                    {
                        reader.Read();
                        origin = JsonSerializer.Deserialize<Vector2>(ref reader, options);
                        haveOrigin = true;
                    }
                    else if (reader.ValueTextEquals("direction"))
                    {
                        reader.Read();
                        direction = JsonSerializer.Deserialize<Vector2>(ref reader, options);
                        haveDirection = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Ray2D");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Ray2D");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Ray2D value,
            JsonSerializerOptions options
        )
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
