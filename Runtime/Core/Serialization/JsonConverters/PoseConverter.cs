// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class PoseConverter : JsonConverter<Pose>
    {
        public static readonly PoseConverter Instance = new();

        private static readonly JsonEncodedText PositionProp = JsonEncodedText.Encode("position");
        private static readonly JsonEncodedText RotationProp = JsonEncodedText.Encode("rotation");

        private PoseConverter() { }

        public override Pose Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 position = default;
            Quaternion rotation = Quaternion.identity;
            bool havePos = false;
            bool haveRot = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Pose(
                        havePos ? position : default,
                        haveRot ? rotation : Quaternion.identity
                    );
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("position"))
                    {
                        reader.Read();
                        position = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        havePos = true;
                    }
                    else if (reader.ValueTextEquals("rotation"))
                    {
                        reader.Read();
                        rotation = JsonSerializer.Deserialize<Quaternion>(ref reader, options);
                        haveRot = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Pose");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Pose");
        }

        public override void Write(Utf8JsonWriter writer, Pose value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PositionProp);
            JsonSerializer.Serialize(writer, value.position, options);
            writer.WritePropertyName(RotationProp);
            JsonSerializer.Serialize(writer, value.rotation, options);
            writer.WriteEndObject();
        }
    }
}
