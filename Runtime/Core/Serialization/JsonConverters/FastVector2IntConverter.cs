namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class FastVector2IntConverter : JsonConverter<FastVector2Int>
    {
        public static readonly FastVector2IntConverter Instance = new();

        private static readonly JsonEncodedText XProp = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText YProp = JsonEncodedText.Encode("y");

        private FastVector2IntConverter() { }

        public override FastVector2Int Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("FastVector2Int must be an object with x and y");
            }

            int x = 0;
            int y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new FastVector2Int(x, y);
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name for FastVector2Int");
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
                else
                {
                    throw new JsonException("Unknown property for FastVector2Int");
                }
            }

            throw new JsonException("Incomplete JSON for FastVector2Int");
        }

        public override void Write(
            Utf8JsonWriter writer,
            FastVector2Int value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(XProp, value.x);
            writer.WriteNumber(YProp, value.y);
            writer.WriteEndObject();
        }
    }
}
