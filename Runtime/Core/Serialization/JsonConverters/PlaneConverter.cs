// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class PlaneConverter : JsonConverter<Plane>
    {
        public static readonly PlaneConverter Instance = new();

        private static readonly JsonEncodedText NormalProp = JsonEncodedText.Encode("normal");
        private static readonly JsonEncodedText DistanceProp = JsonEncodedText.Encode("distance");

        private PlaneConverter() { }

        public override Plane Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 normal = Vector3.up;
            float distance = 0f;
            bool haveNormal = false;
            bool haveDistance = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Plane(
                        haveNormal ? normal : Vector3.up,
                        haveDistance ? distance : 0f
                    );
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("normal"))
                    {
                        reader.Read();
                        normal = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        haveNormal = true;
                    }
                    else if (reader.ValueTextEquals("distance"))
                    {
                        reader.Read();
                        distance = reader.GetSingle();
                        haveDistance = true;
                    }
                    else
                    {
                        throw new JsonException("Unknown property for Plane");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Plane");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Plane value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(NormalProp);
            JsonSerializer.Serialize(writer, value.normal, options);
            writer.WriteNumber(DistanceProp, value.distance);
            writer.WriteEndObject();
        }
    }
}
