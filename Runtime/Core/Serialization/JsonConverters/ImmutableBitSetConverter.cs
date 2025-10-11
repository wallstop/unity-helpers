namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class ImmutableBitSetConverter : JsonConverter<ImmutableBitSet>
    {
        public static readonly ImmutableBitSetConverter Instance = new();

        private static readonly JsonEncodedText CapacityProp = JsonEncodedText.Encode("capacity");
        private static readonly JsonEncodedText SetIndicesProp = JsonEncodedText.Encode(
            "setIndices"
        );

        private ImmutableBitSetConverter() { }

        public override ImmutableBitSet Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token {reader.TokenType} for ImmutableBitSet");
            }

            int capacity = 0;
            System.Collections.Generic.List<int> indices = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    int finalCapacity = capacity;
                    if (indices != null && indices.Count > 0)
                    {
                        int maxIndex = 0;
                        for (int i = 0; i < indices.Count; i++)
                        {
                            if (indices[i] > maxIndex)
                            {
                                maxIndex = indices[i];
                            }
                        }
                        if (finalCapacity < maxIndex + 1)
                        {
                            finalCapacity = maxIndex + 1;
                        }
                    }

                    BitSet bitset = new(finalCapacity > 0 ? finalCapacity : 64);
                    if (indices != null)
                    {
                        for (int i = 0; i < indices.Count; i++)
                        {
                            bitset.TrySet(indices[i]);
                        }
                    }
                    return bitset.ToImmutable();
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in ImmutableBitSet JSON");
                }

                if (reader.ValueTextEquals("capacity"))
                {
                    reader.Read();
                    capacity = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("setIndices"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        throw new JsonException("setIndices must be an array");
                    }
                    indices = new System.Collections.Generic.List<int>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }
                        indices.Add(reader.GetInt32());
                    }
                }
                else
                {
                    throw new JsonException("Unknown property for ImmutableBitSet");
                }
            }

            throw new JsonException("Incomplete JSON for ImmutableBitSet");
        }

        public override void Write(
            Utf8JsonWriter writer,
            ImmutableBitSet value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(CapacityProp, value.Capacity);
            writer.WritePropertyName(SetIndicesProp);
            writer.WriteStartArray();
            foreach (int idx in value.EnumerateSetIndices())
            {
                writer.WriteNumberValue(idx);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
