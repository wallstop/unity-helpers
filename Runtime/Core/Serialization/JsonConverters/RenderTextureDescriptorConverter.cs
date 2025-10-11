// Some properties vary across Unity versions; fields guarded with version defines
namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
#if UNITY_2019_1_OR_NEWER
    using UnityEngine.Rendering;
#endif

    public sealed class RenderTextureDescriptorConverter : JsonConverter<RenderTextureDescriptor>
    {
        public static readonly RenderTextureDescriptorConverter Instance = new();

        private static readonly JsonEncodedText WidthProp = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText HeightProp = JsonEncodedText.Encode("height");
        private static readonly JsonEncodedText MsaaProp = JsonEncodedText.Encode("msaaSamples");
        private static readonly JsonEncodedText DepthProp = JsonEncodedText.Encode("volumeDepth");
        private static readonly JsonEncodedText MipsProp = JsonEncodedText.Encode("mipCount");
        private static readonly JsonEncodedText SrgbProp = JsonEncodedText.Encode("sRGB");
        private static readonly JsonEncodedText UseMipMapProp = JsonEncodedText.Encode("useMipMap");
        private static readonly JsonEncodedText AutoMipsProp = JsonEncodedText.Encode(
            "autoGenerateMips"
        );
        private static readonly JsonEncodedText EnableRWProp = JsonEncodedText.Encode(
            "enableRandomWrite"
        );
        private static readonly JsonEncodedText BindMSProp = JsonEncodedText.Encode("bindMS");
        private static readonly JsonEncodedText DynScaleProp = JsonEncodedText.Encode(
            "useDynamicScale"
        );
#if UNITY_2019_1_OR_NEWER
        private static readonly JsonEncodedText DimProp = JsonEncodedText.Encode("dimension");
        private static readonly JsonEncodedText ShadowSampleProp = JsonEncodedText.Encode(
            "shadowSamplingMode"
        );
        private static readonly JsonEncodedText VRUsageProp = JsonEncodedText.Encode("vrUsage");
        private static readonly JsonEncodedText MemorylessProp = JsonEncodedText.Encode(
            "memoryless"
        );
        private static readonly JsonEncodedText GfxFormatProp = JsonEncodedText.Encode(
            "graphicsFormat"
        );
#endif
#if UNITY_2020_1_OR_NEWER
        private static readonly JsonEncodedText DepthStencilProp = JsonEncodedText.Encode(
            "depthStencilFormat"
        );
#endif

        private RenderTextureDescriptorConverter() { }

        public override RenderTextureDescriptor Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid token type {reader.TokenType}");
            }

            int width = 0,
                height = 0,
                msaa = 1,
                depth = 0,
                mips = 1;
            bool sRGB = false,
                useMipMap = false,
                autoMips = false,
                enableRW = false,
                bindMS = false,
                dynScale = false;
#if UNITY_2019_1_OR_NEWER
            TextureDimension dim = TextureDimension.Tex2D;
            ShadowSamplingMode shadow = ShadowSamplingMode.None;
            VRTextureUsage vrUsage = VRTextureUsage.None;
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None;
            GraphicsFormat gfxFormat = GraphicsFormat.R8G8B8A8_UNorm;
#endif
#if UNITY_2020_1_OR_NEWER
            GraphicsFormat depthStencil = GraphicsFormat.None;
#endif

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    RenderTextureDescriptor desc = new(width, height)
                    {
                        msaaSamples = msaa,
                        volumeDepth = depth,
                        mipCount = mips,
                        sRGB = sRGB,
                        useMipMap = useMipMap,
                        autoGenerateMips = autoMips,
                        enableRandomWrite = enableRW,
                        bindMS = bindMS,
                        useDynamicScale = dynScale,
#if UNITY_2019_1_OR_NEWER
                        dimension = dim,
                        shadowSamplingMode = shadow,
                        vrUsage = vrUsage,
                        memoryless = memoryless,
                        graphicsFormat = gfxFormat,
#endif
#if UNITY_2020_1_OR_NEWER
                        depthStencilFormat = depthStencil,
#endif
                    };
                    return desc;
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
                    else if (reader.ValueTextEquals("msaaSamples"))
                    {
                        reader.Read();
                        msaa = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("volumeDepth"))
                    {
                        reader.Read();
                        depth = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("mipCount"))
                    {
                        reader.Read();
                        mips = reader.GetInt32();
                    }
                    else if (reader.ValueTextEquals("sRGB"))
                    {
                        reader.Read();
                        sRGB = reader.GetBoolean();
                    }
                    else if (reader.ValueTextEquals("useMipMap"))
                    {
                        reader.Read();
                        useMipMap = reader.GetBoolean();
                    }
                    else if (reader.ValueTextEquals("autoGenerateMips"))
                    {
                        reader.Read();
                        autoMips = reader.GetBoolean();
                    }
                    else if (reader.ValueTextEquals("enableRandomWrite"))
                    {
                        reader.Read();
                        enableRW = reader.GetBoolean();
                    }
                    else if (reader.ValueTextEquals("bindMS"))
                    {
                        reader.Read();
                        bindMS = reader.GetBoolean();
                    }
                    else if (reader.ValueTextEquals("useDynamicScale"))
                    {
                        reader.Read();
                        dynScale = reader.GetBoolean();
                    }
#if UNITY_2019_1_OR_NEWER
                    else if (reader.ValueTextEquals("dimension"))
                    {
                        reader.Read();
                        dim = JsonSerializer.Deserialize<TextureDimension>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("shadowSamplingMode"))
                    {
                        reader.Read();
                        shadow = JsonSerializer.Deserialize<ShadowSamplingMode>(
                            ref reader,
                            options
                        );
                    }
                    else if (reader.ValueTextEquals("vrUsage"))
                    {
                        reader.Read();
                        vrUsage = JsonSerializer.Deserialize<VRTextureUsage>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals("memoryless"))
                    {
                        reader.Read();
                        memoryless = JsonSerializer.Deserialize<RenderTextureMemoryless>(
                            ref reader,
                            options
                        );
                    }
                    else if (reader.ValueTextEquals("graphicsFormat"))
                    {
                        reader.Read();
                        gfxFormat = JsonSerializer.Deserialize<GraphicsFormat>(ref reader, options);
                    }
#endif
#if UNITY_2020_1_OR_NEWER
                    else if (reader.ValueTextEquals("depthStencilFormat"))
                    {
                        reader.Read();
                        depthStencil = JsonSerializer.Deserialize<GraphicsFormat>(
                            ref reader,
                            options
                        );
                    }
#endif
                    else
                    {
                        throw new JsonException("Unknown property for RenderTextureDescriptor");
                    }
                }
            }

            throw new JsonException("Incomplete JSON for RenderTextureDescriptor");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RenderTextureDescriptor value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber(WidthProp, value.width);
            writer.WriteNumber(HeightProp, value.height);
            writer.WriteNumber(MsaaProp, value.msaaSamples);
            writer.WriteNumber(DepthProp, value.volumeDepth);
            writer.WriteNumber(MipsProp, value.mipCount);
            writer.WriteBoolean(SrgbProp, value.sRGB);
            writer.WriteBoolean(UseMipMapProp, value.useMipMap);
            writer.WriteBoolean(AutoMipsProp, value.autoGenerateMips);
            writer.WriteBoolean(EnableRWProp, value.enableRandomWrite);
            writer.WriteBoolean(BindMSProp, value.bindMS);
            writer.WriteBoolean(DynScaleProp, value.useDynamicScale);
#if UNITY_2019_1_OR_NEWER
            writer.WritePropertyName(DimProp);
            JsonSerializer.Serialize(writer, value.dimension, options);
            writer.WritePropertyName(ShadowSampleProp);
            JsonSerializer.Serialize(writer, value.shadowSamplingMode, options);
            writer.WritePropertyName(VRUsageProp);
            JsonSerializer.Serialize(writer, value.vrUsage, options);
            writer.WritePropertyName(MemorylessProp);
            JsonSerializer.Serialize(writer, value.memoryless, options);
            writer.WritePropertyName(GfxFormatProp);
            JsonSerializer.Serialize(writer, value.graphicsFormat, options);
#endif
#if UNITY_2020_1_OR_NEWER
            writer.WritePropertyName(DepthStencilProp);
            JsonSerializer.Serialize(writer, value.depthStencilFormat, options);
#endif
            writer.WriteEndObject();
        }
    }
}
