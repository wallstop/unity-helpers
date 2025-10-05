namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Helper;
    using Random;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Utils;

    public enum ColorAveragingMethod
    {
        LAB = 0, // CIE L*a*b* space averaging
        HSV = 1, // HSV space averaging
        Weighted = 2, // Weighted RGB averaging using perceived luminance
        Dominant = 3, // Find most dominant color cluster
    }

    // https://sharpsnippets.wordpress.com/2014/03/11/c-extension-complementary-color/
    public static class ColorExtensions
    {
        public static string ToHex(this Color color, bool includeAlpha = true)
        {
            int r = (int)(Mathf.Clamp01(color.r) * 255f);
            int g = (int)(Mathf.Clamp01(color.g) * 255f);
            int b = (int)(Mathf.Clamp01(color.b) * 255f);

            if (!includeAlpha)
            {
                return $"#{r:X2}{g:X2}{b:X2}";
            }

            int a = (int)(Mathf.Clamp01(color.a) * 255f);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        public static Color GetAverageColor(
            this Sprite sprite,
            ColorAveragingMethod method = ColorAveragingMethod.LAB,
            float alphaCutoff = 0.01f
        )
        {
            return GetAverageColor(Enumerables.Of(sprite), method, alphaCutoff);
        }

        public static Color GetAverageColor(
            this IEnumerable<Sprite> sprites,
            ColorAveragingMethod method = ColorAveragingMethod.LAB,
            float alphaCutoff = 0.01f
        )
        {
            return GetAverageColor(
                sprites
                    .Where(value => value != null)
                    .Select(sprite => sprite.texture)
                    .Where(value => value != null)
                    .SelectMany(texture =>
                    {
                        texture.MakeReadable();
                        Color[] pixels = texture.GetPixels();
                        return pixels;
                    }),
                method,
                alphaCutoff
            );
        }

        public static Color GetAverageColor(
            this IEnumerable<Color> pixels,
            ColorAveragingMethod method = ColorAveragingMethod.LAB,
            float alphaCutoff = 0.01f
        )
        {
            return method switch
            {
                ColorAveragingMethod.LAB => AverageInLABSpace(pixels, alphaCutoff),
                ColorAveragingMethod.HSV => AverageInHSVSpace(pixels, alphaCutoff),
                ColorAveragingMethod.Weighted => WeightedRGBAverage(pixels, alphaCutoff),
                ColorAveragingMethod.Dominant => GetDominantColor(pixels, alphaCutoff),
                _ => throw new InvalidEnumArgumentException(
                    nameof(method),
                    (int)method,
                    typeof(ColorAveragingMethod)
                ),
            };
        }

        // CIE L*a*b* space averaging - most perceptually accurate
        private static Color AverageInLABSpace(IEnumerable<Color> pixels, float alphaCutoff)
        {
            double l = 0;
            double a = 0;
            double b = 0;
            int count = 0;
            switch (pixels)
            {
                case IReadOnlyList<Color> colorList:
                {
                    for (int i = 0; i < colorList.Count; i++)
                    {
                        Color pixel = colorList[i];
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        LABColor lab = RGBToLAB(pixel);
                        l += lab.l;
                        a += lab.a;
                        b += lab.b;
                        ++count;
                    }

                    break;
                }
                case HashSet<Color> colorSet:
                {
                    foreach (Color pixel in colorSet)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        LABColor lab = RGBToLAB(pixel);
                        l += lab.l;
                        a += lab.a;
                        b += lab.b;
                        ++count;
                    }

                    break;
                }
                default:
                {
                    foreach (Color pixel in pixels)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        LABColor lab = RGBToLAB(pixel);
                        l += lab.l;
                        a += lab.a;
                        b += lab.b;
                        ++count;
                    }

                    break;
                }
            }

            count = Mathf.Max(count, 1);
            return LABToRGB(l / count, a / count, b / count);
        }

        // HSV space averaging - good for preserving vibrant colors
        private static Color AverageInHSVSpace(IEnumerable<Color> pixels, float alphaCutoff)
        {
            float sumCos = 0f;
            float sumSin = 0f;
            float sumS = 0f;
            float sumV = 0f;
            int count = 0;

            Accumulate(pixels);

            if (count == 0)
            {
                return Color.black;
            }

            float averageHueRadians = Mathf.Atan2(sumSin / count, sumCos / count);
            if (averageHueRadians < 0f)
            {
                averageHueRadians += 2f * Mathf.PI;
            }

            float averageHue = averageHueRadians / (2f * Mathf.PI);
            float averageS = sumS / count;
            float averageV = sumV / count;

            return Color.HSVToRGB(averageHue, averageS, averageV);

            void Accumulate(IEnumerable<Color> source)
            {
                switch (source)
                {
                    case IReadOnlyList<Color> colorList:
                    {
                        for (int i = 0; i < colorList.Count; ++i)
                        {
                            Color pixel = colorList[i];
                            if (pixel.a <= alphaCutoff)
                            {
                                continue;
                            }

                            Color.RGBToHSV(pixel, out float h, out float s, out float v);
                            float hueRadians = h * 2f * Mathf.PI;
                            sumCos += Mathf.Cos(hueRadians);
                            sumSin += Mathf.Sin(hueRadians);
                            sumS += s;
                            sumV += v;
                            ++count;
                        }

                        break;
                    }
                    case HashSet<Color> colorSet:
                    {
                        foreach (Color pixel in colorSet)
                        {
                            if (pixel.a <= alphaCutoff)
                            {
                                continue;
                            }

                            Color.RGBToHSV(pixel, out float h, out float s, out float v);
                            float hueRadians = h * 2f * Mathf.PI;
                            sumCos += Mathf.Cos(hueRadians);
                            sumSin += Mathf.Sin(hueRadians);
                            sumS += s;
                            sumV += v;
                            ++count;
                        }

                        break;
                    }
                    default:
                    {
                        foreach (Color pixel in source)
                        {
                            if (pixel.a <= alphaCutoff)
                            {
                                continue;
                            }

                            Color.RGBToHSV(pixel, out float h, out float s, out float v);
                            float hueRadians = h * 2f * Mathf.PI;
                            sumCos += Mathf.Cos(hueRadians);
                            sumSin += Mathf.Sin(hueRadians);
                            sumS += s;
                            sumV += v;
                            ++count;
                        }

                        break;
                    }
                }
            }
        }

        // Weighted RGB averaging using perceived luminance
        private static Color WeightedRGBAverage(IEnumerable<Color> pixels, float alphaCutoff)
        {
            // Use perceived luminance weights
            const float rWeight = 0.299f;
            const float gWeight = 0.587f;
            const float bWeight = 0.114f;

            float totalWeight = 0f;
            float r = 0f,
                g = 0f,
                b = 0f,
                a = 0f;

            switch (pixels)
            {
                case IReadOnlyList<Color> colorList:
                {
                    for (int i = 0; i < colorList.Count; i++)
                    {
                        Color pixel = colorList[i];
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        float weight = pixel.r * rWeight + pixel.g * gWeight + pixel.b * bWeight;
                        r += pixel.r * weight;
                        g += pixel.g * weight;
                        b += pixel.b * weight;
                        a += pixel.a * weight;
                        totalWeight += weight;
                    }

                    break;
                }
                case HashSet<Color> colorSet:
                {
                    foreach (Color pixel in colorSet)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }
                        float weight = pixel.r * rWeight + pixel.g * gWeight + pixel.b * bWeight;
                        r += pixel.r * weight;
                        g += pixel.g * weight;
                        b += pixel.b * weight;
                        a += pixel.a * weight;
                        totalWeight += weight;
                    }

                    break;
                }
                default:
                {
                    foreach (Color pixel in pixels)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        float weight = pixel.r * rWeight + pixel.g * gWeight + pixel.b * bWeight;
                        r += pixel.r * weight;
                        g += pixel.g * weight;
                        b += pixel.b * weight;
                        a += pixel.a * weight;
                        totalWeight += weight;
                    }

                    break;
                }
            }

            if (totalWeight > 0f)
            {
                r /= totalWeight;
                g /= totalWeight;
                b /= totalWeight;
                a /= totalWeight;
            }

            return new Color(r, g, b, a);
        }

        // Find dominant color using simple clustering
        private static Color GetDominantColor(IEnumerable<Color> pixels, float alphaCutoff)
        {
            using PooledResource<Dictionary<FastVector3Int, int>> colorBucketResource =
                DictionaryBuffer<FastVector3Int, int>.Dictionary.Get(
                    out Dictionary<FastVector3Int, int> cache
                );
            const int bucketSize = 32; // Adjust for different precision

            switch (pixels)
            {
                case IReadOnlyList<Color> colorList:
                {
                    for (int i = 0; i < colorList.Count; i++)
                    {
                        Color pixel = colorList[i];
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        FastVector3Int bucket = new(
                            Mathf.RoundToInt(pixel.r * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.g * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.b * 255 / bucketSize)
                        );

                        cache.AddOrUpdate(bucket, _ => 0, (_, value) => value + 1);
                    }

                    break;
                }
                case HashSet<Color> colorSet:
                {
                    foreach (Color pixel in colorSet)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        FastVector3Int bucket = new(
                            Mathf.RoundToInt(pixel.r * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.g * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.b * 255 / bucketSize)
                        );

                        cache.AddOrUpdate(bucket, _ => 0, (_, value) => value + 1);
                    }

                    break;
                }
                default:
                {
                    foreach (Color pixel in pixels)
                    {
                        if (pixel.a <= alphaCutoff)
                        {
                            continue;
                        }

                        FastVector3Int bucket = new(
                            Mathf.RoundToInt(pixel.r * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.g * 255 / bucketSize),
                            Mathf.RoundToInt(pixel.b * 255 / bucketSize)
                        );

                        cache.AddOrUpdate(bucket, _ => 0, (_, value) => value + 1);
                    }

                    break;
                }
            }

            KeyValuePair<FastVector3Int, int>? largest = null;
            if (0 < cache.Count)
            {
                foreach (KeyValuePair<FastVector3Int, int> bucketEntry in cache)
                {
                    largest ??= bucketEntry;
                    if (largest.Value.Value < bucketEntry.Value)
                    {
                        largest = bucketEntry;
                    }
                }
            }

            if (largest == null)
            {
                return default;
            }

            FastVector3Int dominantBucket = largest.Value.Key;
            return new Color(
                dominantBucket.x * bucketSize / 255f,
                dominantBucket.y * bucketSize / 255f,
                dominantBucket.z * bucketSize / 255f
            );
        }

        // Helper struct for LAB color space
        private readonly struct LABColor
        {
            public readonly double l;
            public readonly double a;
            public readonly double b;

            public LABColor(double l, double a, double b)
            {
                this.l = l;
                this.a = a;
                this.b = b;
            }
        }

        private static LABColor RGBToLAB(Color rgb)
        {
            // First convert to XYZ
            double r =
                rgb.r > 0.04045 ? Mathf.Pow((rgb.r + 0.055f) / 1.055f, 2.4f) : rgb.r / 12.92f;
            double g =
                rgb.g > 0.04045 ? Mathf.Pow((rgb.g + 0.055f) / 1.055f, 2.4f) : rgb.g / 12.92f;
            double b =
                rgb.b > 0.04045 ? Mathf.Pow((rgb.b + 0.055f) / 1.055f, 2.4f) : rgb.b / 12.92f;

            double x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047;
            double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883;

            x = x > 0.008856 ? Mathf.Pow((float)x, 1f / 3f) : 7.787 * x + 16f / 116f;
            y = y > 0.008856 ? Mathf.Pow((float)y, 1f / 3f) : 7.787 * y + 16f / 116f;
            z = z > 0.008856 ? Mathf.Pow((float)z, 1f / 3f) : 7.787 * z + 16f / 116f;

            return new LABColor(116 * y - 16, 500 * (x - y), 200 * (y - z));
        }

        private static Color LABToRGB(double l, double a, double b)
        {
            double y = (l + 16) / 116;
            double x = a / 500 + y;
            double z = y - b / 200;

            double x3 = x * x * x;
            double y3 = y * y * y;
            double z3 = z * z * z;

            x = 0.95047 * (x3 > 0.008856 ? x3 : (x - 16.0 / 116.0) / 7.787);
            y = y3 > 0.008856 ? y3 : (y - 16.0 / 116.0) / 7.787;
            z = 1.08883 * (z3 > 0.008856 ? z3 : (z - 16.0 / 116.0) / 7.787);

            double r = x * 3.2406 + y * -1.5372 + z * -0.4986;
            double g = x * -0.9689 + y * 1.8758 + z * 0.0415;
            double b2 = x * 0.0557 + y * -0.2040 + z * 1.0570;

            r = r > 0.0031308 ? 1.055 * Mathf.Pow((float)r, 1 / 2.4f) - 0.055 : 12.92 * r;
            g = g > 0.0031308 ? 1.055 * Mathf.Pow((float)g, 1 / 2.4f) - 0.055 : 12.92 * g;
            b2 = b2 > 0.0031308 ? 1.055 * Mathf.Pow((float)b2, 1 / 2.4f) - 0.055 : 12.92 * b2;

            return new Color(
                Mathf.Clamp01((float)r),
                Mathf.Clamp01((float)g),
                Mathf.Clamp01((float)b2),
                1f
            );
        }

        public static Color GetComplement(
            this Color source,
            IRandom random = null,
            float variance = 0f
        )
        {
            Color inputColor = source;
            /*
                If RGB values are close to each other by a diff less than 10%, then if RGB values are lighter side,
                decrease the blue by 50% (eventually it will increase in conversion below), if RBB values are on the
                darker side, decrease yellow by about 50% (it will increase in conversion)
             */
            float avgColorValue = (source.r + source.g + source.b) / 3;
            float rDiff = Mathf.Abs(source.r - avgColorValue);
            float gDiff = Mathf.Abs(source.g - avgColorValue);
            float bDiff = Mathf.Abs(source.b - avgColorValue);
            const float greyDelta = 20 / 255f;
            //The color is a shade of gray
            if (rDiff < greyDelta && gDiff < greyDelta && bDiff < greyDelta)
            {
                // Color is dark
                if (avgColorValue < 123 / 255f)
                {
                    inputColor.b = 220 / 255f;
                    inputColor.g = 230 / 255f;
                    inputColor.r = 50 / 255f;
                }
                else
                {
                    inputColor.r = 255 / 255f;
                    inputColor.g = 255 / 255f;
                    inputColor.b = 50 / 255f;
                }
            }

            if (random != null)
            {
                if (variance != 0)
                {
                    variance = Mathf.Abs(variance);

                    float minR = Mathf.Clamp01(inputColor.r - variance);
                    float maxR = Mathf.Clamp01(inputColor.r + variance);
                    inputColor.r = random.NextFloat(minR, maxR);

                    float minG = Mathf.Clamp01(inputColor.g - variance);
                    float maxG = Mathf.Clamp01(inputColor.g + variance);
                    inputColor.g = random.NextFloat(minG, maxG);

                    float minB = Mathf.Clamp01(inputColor.b - variance);
                    float maxB = Mathf.Clamp01(inputColor.b + variance);
                    inputColor.b = random.NextFloat(minB, maxB);
                }
                else
                {
                    inputColor.r *= random.NextFloat(1 / inputColor.r);
                    inputColor.g *= random.NextFloat(1 / inputColor.g);
                    inputColor.b *= random.NextFloat(1 / inputColor.b);
                }
            }

            Color.RGBToHSV(inputColor, out float h, out float s, out float v);
            h = h < 0.5f ? h + 0.5f : h - 0.5f;
            Color result = Color.HSVToRGB(h, s, v);
            return result;
        }
    }
}
