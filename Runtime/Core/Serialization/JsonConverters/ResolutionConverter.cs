// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class ResolutionConverter : JsonConverter<Resolution>
    {
        public static readonly ResolutionConverter Instance = new();

        private static readonly JsonEncodedText WidthProp = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText HeightProp = JsonEncodedText.Encode("height");
        private static readonly JsonEncodedText RefreshRateProp = JsonEncodedText.Encode(
            "refreshRate"
        );
#if UNITY_2022_2_OR_NEWER
        private static readonly JsonEncodedText RefreshRatioProp = JsonEncodedText.Encode(
            "refreshRateRatio"
        );
        private static readonly JsonEncodedText RatioNumProp = JsonEncodedText.Encode("numerator");
        private static readonly JsonEncodedText RatioDenProp = JsonEncodedText.Encode(
            "denominator"
        );
        private static readonly JsonEncodedText RatioValProp = JsonEncodedText.Encode("value");
#endif

        private ResolutionConverter() { }

        public override Resolution Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            int width = 0;
            int height = 0;
            int refreshHz = 0;
#if UNITY_2022_2_OR_NEWER
            int ratioNum = 0;
            int ratioDen = 0;
#endif

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    Resolution r = new() { width = width, height = height };
#if !UNITY_2022_2_OR_NEWER
                    r.refreshRate = refreshHz;
#endif
                    return r;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("width"))
                    {
                        reader.Read();
                        width = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("height"))
                    {
                        reader.Read();
                        height = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("refreshRate"))
                    {
                        reader.Read();
                        refreshHz = reader.GetInt32();
                    }
#if UNITY_2022_2_OR_NEWER
                    else if (reader.ValueTextEquals("refreshRateRatio"))
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndObject)
                                {
                                    break;
                                }

                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    if (reader.ValueTextEquals("numerator"))
                                    {
                                        reader.Read();
                                        ratioNum = reader.GetInt32();
                                    }
                                    else if (reader.ValueTextEquals("denominator"))
                                    {
                                        reader.Read();
                                        ratioDen = reader.GetInt32();
                                    }
                                    else if (reader.ValueTextEquals("value"))
                                    {
                                        reader.Read(); /* ignore float value; num/den preferred */
                                        reader.GetSingle();
                                    }
                                    else
                                    {
                                        throw new JsonException(
                                            "Unknown property for refreshRateRatio"
                                        );
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new JsonException("refreshRateRatio must be an object");
                        }
                    }
#endif
                    else
                    {
                        throw new JsonException("Unknown property for Resolution");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for Resolution");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Resolution value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(WidthProp, value.width);
            writer.WriteNumber(HeightProp, value.height);
#if UNITY_2022_2_OR_NEWER
            uint num = value.refreshRateRatio.numerator;
            uint den = value.refreshRateRatio.denominator;
            int hz = den != 0 ? (int)Math.Round((double)num / den) : 0;
            writer.WriteNumber(RefreshRateProp, hz);
            writer.WritePropertyName(RefreshRatioProp);
            writer.WriteStartObject();
            writer.WriteNumber(RatioNumProp, num);
            writer.WriteNumber(RatioDenProp, den);
            // Omit "value" to avoid NaN/Infinity writes; consumers can compute from num/den
            writer.WriteEndObject();
#else
            writer.WriteNumber(RefreshRateProp, value.refreshRate);
#endif
            writer.WriteEndObject();
        }
    }
}
