namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class TextureScaleTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [TestCase(true)]
        [TestCase(false)]
        public void ScaleThrowsWhenTextureIsNull(bool useBilinear)
        {
            Assert.Throws<ArgumentNullException>(() => InvokeScale(null, 2, 2, useBilinear));
        }

        [TestCase(true, 0)]
        [TestCase(true, -3)]
        [TestCase(false, 0)]
        [TestCase(false, -3)]
        public void ScaleThrowsWhenWidthIsNotPositive(bool useBilinear, int newWidth)
        {
            Texture2D texture = CreateTexture(2, 2, (x, y) => new Color(x, y, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                InvokeScale(texture, newWidth, 2, useBilinear)
            );
        }

        [TestCase(true, 0)]
        [TestCase(true, -5)]
        [TestCase(false, 0)]
        [TestCase(false, -5)]
        public void ScaleThrowsWhenHeightIsNotPositive(bool useBilinear, int newHeight)
        {
            Texture2D texture = CreateTexture(2, 2, (x, y) => new Color(x, y, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                InvokeScale(texture, 2, newHeight, useBilinear)
            );
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ScaleThrowsWhenTextureIsNotReadable(bool useBilinear)
        {
            Texture2D texture = CreateTexture(2, 2, (x, y) => new Color(x, y, 0f, 1f));
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            Assert.Throws<UnityException>(() => InvokeScale(texture, 1, 1, useBilinear));
        }

        [Test]
        public void PointDownscaleProducesNearestNeighborSamples()
        {
            Texture2D texture = CreateTexture(
                5,
                4,
                (x, y) => new Color(x / 10f, y / 10f, (x + y) / 20f, (x + 1) / 5f)
            );
            Color[] source = texture.GetPixels();
            Color[] expected = ComputeNearestNeighbor(source, 5, 4, 3, 2);

            TextureScale.Point(texture, 3, 2);

            Assert.AreEqual(3, texture.width);
            Assert.AreEqual(2, texture.height);
            Color[] actual = texture.GetPixels();
            AssertColorsEqual(actual, expected);
        }

        [Test]
        public void PointUpscaleDuplicatesSourcePixels()
        {
            Texture2D texture = CreateTexture(
                2,
                3,
                (x, y) => new Color(x * 0.25f, y * 0.2f, (x + y) * 0.1f, 1f - 0.1f * x)
            );
            Color[] source = texture.GetPixels();
            Color[] expected = ComputeNearestNeighbor(source, 2, 3, 5, 7);

            TextureScale.Point(texture, 5, 7);

            Assert.AreEqual(5, texture.width);
            Assert.AreEqual(7, texture.height);
            Color[] actual = texture.GetPixels();
            AssertColorsEqual(actual, expected);
        }

        [Test]
        public void PointScalingKeepsTextureReadable()
        {
            Texture2D texture = CreateTexture(3, 3, (x, y) => new Color(x, y, 0f, 1f));
            TextureScale.Point(texture, 3, 3);
            Assert.IsTrue(texture.isReadable);
        }

        [Test]
        public void BilinearDownscaleInterpolatesBetweenSourcePixels()
        {
            Texture2D texture = CreateTexture(
                4,
                3,
                (x, y) => new Color(x / 5f, y / 4f, (x * y) / 20f, ((x + y) % 4) / 4f)
            );
            Color[] source = texture.GetPixels();
            Color[] expected = ComputeBilinear(source, 4, 3, 2, 2);

            TextureScale.Bilinear(texture, 2, 2);

            Assert.AreEqual(2, texture.width);
            Assert.AreEqual(2, texture.height);
            Color[] actual = texture.GetPixels();
            AssertColorsEqual(actual, expected, 5e-5f);
        }

        [Test]
        public void BilinearUpscaleInterpolatesSmoothly()
        {
            Texture2D texture = CreateTexture(
                3,
                2,
                (x, y) => new Color(x / 3f, y / 2f, (x + y) / 6f, 1f)
            );
            Color[] source = texture.GetPixels();
            Color[] expected = ComputeBilinear(source, 3, 2, 6, 4);

            TextureScale.Bilinear(texture, 6, 4);

            Assert.AreEqual(6, texture.width);
            Assert.AreEqual(4, texture.height);
            Color[] actual = texture.GetPixels();
            AssertColorsEqual(actual, expected, 5e-5f);
        }

        [Test]
        public void BilinearClampsSamplingAtTextureEdges()
        {
            Texture2D texture = CreateTexture(
                2,
                2,
                (x, y) => new Color(x == 0 ? 0f : 1f, y == 0 ? 0f : 1f, (x + y) / 3f, 1f)
            );
            Color[] source = texture.GetPixels();
            Color[] expected = ComputeBilinear(source, 2, 2, 5, 5);

            TextureScale.Bilinear(texture, 5, 5);

            Color[] actual = texture.GetPixels();
            AssertColorsEqual(actual, expected, 5e-5f);

            int lastIndex = Index(4, 4, 5);
            AssertColor(actual[lastIndex], expected[lastIndex], 5e-5f);
        }

        private Texture2D CreateTexture(int width, int height, Func<int, int, Color> pixelFactory)
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
            return Track(texture);
        }

        private static void InvokeScale(Texture2D texture, int width, int height, bool useBilinear)
        {
            if (useBilinear)
            {
                TextureScale.Bilinear(texture, width, height);
            }
            else
            {
                TextureScale.Point(texture, width, height);
            }
        }

        private static Color[] ComputeNearestNeighbor(
            Color[] source,
            int sourceWidth,
            int sourceHeight,
            int destWidth,
            int destHeight
        )
        {
            Color[] dest = new Color[destWidth * destHeight];
            float ratioX = (float)sourceWidth / destWidth;
            float ratioY = (float)sourceHeight / destHeight;

            for (int y = 0; y < destHeight; ++y)
            {
                int sourceY = (int)(ratioY * y);
                int sourceRow = sourceY * sourceWidth;
                int destRow = y * destWidth;
                for (int x = 0; x < destWidth; ++x)
                {
                    int sourceX = (int)(ratioX * x);
                    dest[destRow + x] = source[sourceRow + sourceX];
                }
            }

            return dest;
        }

        private static Color[] ComputeBilinear(
            Color[] source,
            int sourceWidth,
            int sourceHeight,
            int destWidth,
            int destHeight
        )
        {
            Color[] dest = new Color[destWidth * destHeight];
            float ratioX = (float)(sourceWidth - 1) / destWidth;
            float ratioY = (float)(sourceHeight - 1) / destHeight;
            int maxSourceX = sourceWidth - 1;
            int maxSourceY = sourceHeight - 1;

            for (int y = 0; y < destHeight; ++y)
            {
                float sourceYFloat = y * ratioY;
                int sourceY = (int)sourceYFloat;
                float yLerp = sourceYFloat - sourceY;
                int sourceY1 = Math.Min(sourceY, maxSourceY);
                int sourceY2 = Math.Min(sourceY + 1, maxSourceY);
                int y1Offset = sourceY1 * sourceWidth;
                int y2Offset = sourceY2 * sourceWidth;
                int destRow = y * destWidth;

                for (int x = 0; x < destWidth; ++x)
                {
                    float sourceXFloat = x * ratioX;
                    int sourceX = (int)sourceXFloat;
                    float xLerp = sourceXFloat - sourceX;
                    int sourceX1 = Math.Min(sourceX, maxSourceX);
                    int sourceX2 = Math.Min(sourceX + 1, maxSourceX);

                    Color c11 = source[y1Offset + sourceX1];
                    Color c21 = source[y1Offset + sourceX2];
                    Color c12 = source[y2Offset + sourceX1];
                    Color c22 = source[y2Offset + sourceX2];

                    Color top = Color.LerpUnclamped(c11, c21, xLerp);
                    Color bottom = Color.LerpUnclamped(c12, c22, xLerp);
                    dest[destRow + x] = Color.LerpUnclamped(top, bottom, yLerp);
                }
            }

            return dest;
        }

        private static void AssertColorsEqual(
            Color[] actual,
            Color[] expected,
            float tolerance = 1e-5f
        )
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; ++i)
            {
                AssertColor(actual[i], expected[i], tolerance);
            }
        }

        private static void AssertColor(Color actual, Color expected, float tolerance = 1e-5f)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        private static int Index(int x, int y, int width)
        {
            return y * width + x;
        }
    }
}
