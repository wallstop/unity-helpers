namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class MinMaxGradientConverter : JsonConverter<ParticleSystem.MinMaxGradient>
    {
        public static readonly MinMaxGradientConverter Instance = new();

        private static readonly JsonEncodedText ModeProp = JsonEncodedText.Encode("mode");
        private static readonly JsonEncodedText ColorProp = JsonEncodedText.Encode("color");
        private static readonly JsonEncodedText ColorMinProp = JsonEncodedText.Encode("colorMin");
        private static readonly JsonEncodedText ColorMaxProp = JsonEncodedText.Encode("colorMax");
        private static readonly JsonEncodedText GradientProp = JsonEncodedText.Encode("gradient");
        private static readonly JsonEncodedText GradientMinProp = JsonEncodedText.Encode(
            "gradientMin"
        );
        private static readonly JsonEncodedText GradientMaxProp = JsonEncodedText.Encode(
            "gradientMax"
        );

        private MinMaxGradientConverter() { }

        public override ParticleSystem.MinMaxGradient Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            ParticleSystemGradientMode mode = ParticleSystemGradientMode.Color;
            Color color = default,
                colorMin = default,
                colorMax = default;
            Gradient gradient = null,
                gradientMin = null,
                gradientMax = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    ParticleSystem.MinMaxGradient mmg = new(color)
                    {
                        mode = mode,
                        color = color,
                        colorMin = colorMin,
                        colorMax = colorMax,
                        gradient = gradient,
                        gradientMin = gradientMin,
                        gradientMax = gradientMax,
                    };
                    return mmg;
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("mode"))
                    {
                        reader.Read();
                        mode = JsonSerializer.Deserialize<ParticleSystemGradientMode>(
                            ref reader,
                            options
                        );
                    }
                    else if (reader.ValueTextEquals("color"))
                    {
                        reader.Read();
                        color = JsonSerializer.Deserialize<Color>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("colorMin"))
                    {
                        reader.Read();
                        colorMin = JsonSerializer.Deserialize<Color>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("colorMax"))
                    {
                        reader.Read();
                        colorMax = JsonSerializer.Deserialize<Color>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("gradient"))
                    {
                        reader.Read();
                        gradient = JsonSerializer.Deserialize<Gradient>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("gradientMin"))
                    {
                        reader.Read();
                        gradientMin = JsonSerializer.Deserialize<Gradient>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("gradientMax"))
                    {
                        reader.Read();
                        gradientMax = JsonSerializer.Deserialize<Gradient>(ref reader, options);
                    }
                    else
                    {
                        throw new JsonException("Unknown property for MinMaxGradient");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for MinMaxGradient");
        }

        public override void Write(
            Utf8JsonWriter writer,
            ParticleSystem.MinMaxGradient value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ModeProp);
            JsonSerializer.Serialize(writer, value.mode, options);
            writer.WritePropertyName(ColorProp);
            JsonSerializer.Serialize(writer, value.color, options);
            writer.WritePropertyName(ColorMinProp);
            JsonSerializer.Serialize(writer, value.colorMin, options);
            writer.WritePropertyName(ColorMaxProp);
            JsonSerializer.Serialize(writer, value.colorMax, options);
            writer.WritePropertyName(GradientProp);
            JsonSerializer.Serialize(writer, value.gradient, options);
            writer.WritePropertyName(GradientMinProp);
            JsonSerializer.Serialize(writer, value.gradientMin, options);
            writer.WritePropertyName(GradientMaxProp);
            JsonSerializer.Serialize(writer, value.gradientMax, options);
            writer.WriteEndObject();
        }
    }
}
