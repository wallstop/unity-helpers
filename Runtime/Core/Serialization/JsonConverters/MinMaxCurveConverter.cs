// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class MinMaxCurveConverter : JsonConverter<ParticleSystem.MinMaxCurve>
    {
        public static readonly MinMaxCurveConverter Instance = new();

        private static readonly JsonEncodedText ModeProp = JsonEncodedText.Encode("mode");
        private static readonly JsonEncodedText ConstantProp = JsonEncodedText.Encode("constant");
        private static readonly JsonEncodedText ConstantMinProp = JsonEncodedText.Encode(
            "constantMin"
        );
        private static readonly JsonEncodedText ConstantMaxProp = JsonEncodedText.Encode(
            "constantMax"
        );
        private static readonly JsonEncodedText CurveProp = JsonEncodedText.Encode("curve");
        private static readonly JsonEncodedText CurveMinProp = JsonEncodedText.Encode("curveMin");
        private static readonly JsonEncodedText CurveMaxProp = JsonEncodedText.Encode("curveMax");
        private static readonly JsonEncodedText MultiplierProp = JsonEncodedText.Encode(
            "multiplier"
        );

        private MinMaxCurveConverter() { }

        public override ParticleSystem.MinMaxCurve Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            ParticleSystemCurveMode mode = ParticleSystemCurveMode.Constant;
            float constant = 0f;
            float constantMin = 0f;
            float constantMax = 0f;
            float multiplier = 1f;
            AnimationCurve curve = null;
            AnimationCurve curveMin = null;
            AnimationCurve curveMax = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    ParticleSystem.MinMaxCurve mmc = new(multiplier)
                    {
                        mode = mode,
                        constant = constant,
                        constantMin = constantMin,
                        constantMax = constantMax,
                        curve = curve,
                        curveMin = curveMin,
                        curveMax = curveMax,
                    };
                    return mmc;
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("mode"))
                    {
                        reader.Read();
                        mode = JsonSerializer.Deserialize<ParticleSystemCurveMode>(
                            ref reader,
                            options
                        );
                    }
                    else if (reader.ValueTextEquals("constant"))
                    {
                        reader.Read();
                        constant = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("constantMin"))
                    {
                        reader.Read();
                        constantMin = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("constantMax"))
                    {
                        reader.Read();
                        constantMax = reader.GetSingle();
                    }
                    else if (reader.ValueTextEquals("curve"))
                    {
                        reader.Read();
                        curve = JsonSerializer.Deserialize<AnimationCurve>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("curveMin"))
                    {
                        reader.Read();
                        curveMin = JsonSerializer.Deserialize<AnimationCurve>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("curveMax"))
                    {
                        reader.Read();
                        curveMax = JsonSerializer.Deserialize<AnimationCurve>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("multiplier"))
                    {
                        reader.Read();
                        multiplier = reader.GetSingle();
                    }
                    else
                    {
                        throw new JsonException("Unknown property for MinMaxCurve");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for MinMaxCurve");
        }

        public override void Write(
            Utf8JsonWriter writer,
            ParticleSystem.MinMaxCurve value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ModeProp);
            JsonSerializer.Serialize(writer, value.mode, options);
            writer.WriteNumber(ConstantProp, value.constant);
            writer.WriteNumber(ConstantMinProp, value.constantMin);
            writer.WriteNumber(ConstantMaxProp, value.constantMax);
            writer.WritePropertyName(CurveProp);
            JsonSerializer.Serialize(writer, value.curve, options);
            writer.WritePropertyName(CurveMinProp);
            JsonSerializer.Serialize(writer, value.curveMin, options);
            writer.WritePropertyName(CurveMaxProp);
            JsonSerializer.Serialize(writer, value.curveMax, options);
            writer.WriteNumber(MultiplierProp, value.curveMultiplier);
            writer.WriteEndObject();
        }
    }
}
