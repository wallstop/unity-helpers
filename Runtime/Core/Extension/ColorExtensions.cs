namespace Core.Extension
{
    using System;
    using Random;
    using UnityEngine;

    // https://sharpsnippets.wordpress.com/2014/03/11/c-extension-complementary-color/
    public static class ColorExtensions
    {
        public static Color GetComplement(this Color source, IRandom random = null, float variance = 0f)
        {
            Color inputColor = source;
            //if RGB values are close to each other by a diff less than 10%, then if RGB values are lighter side, decrease the blue by 50% (eventually it will increase in conversion below), if RBB values are on darker side, decrease yellow by about 50% (it will increase in conversion)
            float avgColorValue = (source.r + source.g + source.b) / 3;
            float rDiff = Math.Abs(source.r - avgColorValue);
            float gDiff = Math.Abs(source.g - avgColorValue);
            float bDiff = Math.Abs(source.b - avgColorValue);
            const float greyDelta = 20 / 255f;
            if (rDiff < greyDelta && gDiff < greyDelta && bDiff < greyDelta) //The color is a shade of gray
            {
                if (avgColorValue < 123 / 255f) //color is dark
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
                    variance = Math.Abs(variance);

                    float minR = Clamp(inputColor.r - variance);
                    float maxR = Clamp(inputColor.r + variance);
                    inputColor.r = random.NextFloat(minR, maxR);

                    float minG = Clamp(inputColor.g - variance);
                    float maxG = Clamp(inputColor.g + variance);
                    inputColor.g = random.NextFloat(minG, maxG);

                    float minB = Clamp(inputColor.b - variance);
                    float maxB = Clamp(inputColor.b + variance);
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

        private static float Clamp(float value)
        {
            return Math.Clamp(value, 0, 1);
        }
    }
}
