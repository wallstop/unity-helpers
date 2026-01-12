// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class FastVector3IntConverter : JsonConverter<FastVector3Int>
    {
        public static readonly FastVector3IntConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText ZProp = JsonEncodedText.Encode("z");

        private FastVector3IntConverter() { }

        public override FastVector3Int Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("FastVector3Int must be an object with x, y and z");
            }

            int x = 0;
            int y = 0;
            int z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new FastVector3Int(x, y, z);
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name for FastVector3Int");
                }
                if (reader.ValueTextEquals("x"))
                {
                    reader.Read();
                    x = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("y"))
                {
                    reader.Read();
                    y = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("z"))
                {
                    reader.Read();
                    z = reader.GetInt32();
                }
                else
                {
                    throw new JsonException("Unknown property for FastVector3Int");
                }
            }

            throw new JsonException("Incomplete JSON for FastVector3Int");
        }

        public override void Write(
            Utf8JsonWriter writer,
            FastVector3Int value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteNumber(ZProp, value.z);
            writer.WriteEndObject();
        }
    }
}
