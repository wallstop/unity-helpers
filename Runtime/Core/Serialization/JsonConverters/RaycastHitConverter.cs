namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class RaycastHitConverter : JsonConverter<RaycastHit>
    {
        public static readonly RaycastHitConverter Instance = new();

        private static readonly JsonEncodedText PointProp = JsonEncodedText.Encode("point");
        private static readonly JsonEncodedText NormalProp = JsonEncodedText.Encode("normal");
        private static readonly JsonEncodedText DistanceProp = JsonEncodedText.Encode("distance");
        private static readonly JsonEncodedText TriangleIndexProp = JsonEncodedText.Encode(
            "triangleIndex"
        );

        private RaycastHitConverter() { }

        public override RaycastHit Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            Vector3 point = default;
            Vector3 normal = default;
            float distance = 0f;
            bool havePoint = false;
            bool haveNormal = false;
            bool haveDistance = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    RaycastHit hit = new();
                    if (havePoint)
                    {
                        hit.point = point;
                    }

                    if (haveNormal)
                    {
                        hit.normal = normal;
                    }

                    if (haveDistance)
                    {
                        hit.distance = distance;
                    }

                    // triangleIndex is read-only; cannot assign on RaycastHit
                    return hit;
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("point"))
                    {
                        reader.Read();
                        point = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                        havePoint = true;
                    }
                    else if (reader.ValueTextEquals("normal"))
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
                    else if (reader.ValueTextEquals("triangleIndex"))
                    {
                        reader.Read();
                        reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for RaycastHit");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for RaycastHit");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RaycastHit value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PointProp);
            JsonSerializer.Serialize(writer, value.point, options);
            writer.WritePropertyName(NormalProp);
            JsonSerializer.Serialize(writer, value.normal, options);
            writer.WriteNumber(DistanceProp, value.distance);
#if UNITY_2019_1_OR_NEWER
            writer.WriteNumber(TriangleIndexProp, value.triangleIndex);
#endif
            writer.WriteEndObject();
        }
    }
}
