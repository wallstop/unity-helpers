namespace WallstopStudios.UnityHelpers.Tests.Editor.Tools
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Tools;

    public sealed class ImageBlurToolTests
    {
        [Test]
        public void KernelHasExpectedLengthAndNormalizes()
        {
            for (int radius = 1; radius <= 4; radius++)
            {
                float[] kernel = ImageBlurTool.KernelForTests(radius);
                Assert.NotNull(kernel);
                Assert.AreEqual(radius * 2 + 1, kernel.Length);
                float sum = 0f;
                for (int i = 0; i < kernel.Length; i++)
                {
                    sum += kernel[i];
                }
                Assert.That(sum, Is.InRange(0.999f, 1.001f));
            }
        }

        [Test]
        public void BlurredTextureMatchesInputDimensions()
        {
            Texture2D tex = new(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();

            Texture2D blurred = ImageBlurTool.BlurredForTests(tex, 2);
            Assert.IsTrue(blurred != null);
            Assert.AreEqual(tex.width, blurred.width);
            Assert.AreEqual(tex.height, blurred.height);
        }
    }
#endif
}
