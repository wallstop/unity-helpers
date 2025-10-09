namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Visuals.UIToolkit;
    using Object = UnityEngine.Object;

    internal static class VisualsTestHelpers
    {
        public static Sprite CreateSprite(
            List<Object> tracked,
            int width,
            int height,
            Func<int, int, Color> pixelFactory,
            float pixelsPerUnit = 1f,
            Vector2? pivot = null
        )
        {
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: false
            )
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; ++y)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; ++x)
                {
                    pixels[rowOffset + x] = pixelFactory(x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            Vector2 pivotValue = pivot ?? new Vector2(0.5f, 0.5f);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                pivotValue,
                pixelsPerUnit
            );

            tracked.Add(texture);
            tracked.Add(sprite);
            return sprite;
        }

        public static Texture2D[] GetComputedTextures(LayeredImage image, List<Object> tracked)
        {
            Texture2D[] computed = (Texture2D[])
                typeof(LayeredImage)
                    .GetField(
                        "_computed",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )
                    .GetValue(image);

            foreach (Texture2D texture in computed)
            {
                if (texture != null)
                {
                    tracked.Add(texture);
                }
            }

            return computed;
        }

        public static Color32[] GetPixelData(Texture2D texture)
        {
            if (texture == null)
            {
                return Array.Empty<Color32>();
            }

            Color32[] pixels = texture.GetPixels32();
            if (pixels == null || pixels.Length == 0)
            {
                return Array.Empty<Color32>();
            }

            return pixels;
        }

        public static Color32 GetPixel(Texture2D texture, int x, int y)
        {
            Color32[] pixels = GetPixelData(texture);
            if (pixels.Length == 0)
            {
                return default;
            }

            int width = texture.width;
            return pixels[y * width + x];
        }

        public static void DestroyTracked(List<Object> tracked)
        {
            for (int i = tracked.Count - 1; i >= 0; --i)
            {
                Object instance = tracked[i];
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            tracked.Clear();
        }

        public static void AssertColor(Color32 actual, Color32 expected, byte tolerance = 1)
        {
            Assert.That(Mathf.Abs(actual.r - expected.r), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(actual.g - expected.g), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(actual.b - expected.b), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(actual.a - expected.a), Is.LessThanOrEqualTo(tolerance));
        }

        public static void AssertVector(Vector2 actual, Vector2 expected, float tolerance = 1e-3f)
        {
            Assert.That(
                (actual - expected).sqrMagnitude,
                Is.LessThanOrEqualTo(tolerance * tolerance)
            );
        }
    }
}
