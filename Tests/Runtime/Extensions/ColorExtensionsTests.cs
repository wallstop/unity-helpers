namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class ColorExtensionsTests
    {
        [Test]
        public void ToHexFormatsCorrectly()
        {
            Color color = new(0.1f, 0.2f, 0.3f, 0.4f);
            Assert.AreEqual("#19334C66", color.ToHex());
            Assert.AreEqual("#19334C", color.ToHex(includeAlpha: false));
        }

        [Test]
        public void GetAverageColorReturnsSameColorForUniformInput()
        {
            Color input = new(0.25f, 0.5f, 0.75f, 1f);
            Color[] pixels = { input, input, input };

            foreach (
                ColorAveragingMethod method in System.Enum.GetValues(typeof(ColorAveragingMethod))
            )
            {
                Color result = pixels.GetAverageColor(method);
                Assert.AreEqual(input.r, result.r, 1e-2f, method + " R");
                Assert.AreEqual(input.g, result.g, 1e-2f, method + " G");
                Assert.AreEqual(input.b, result.b, 1e-2f, method + " B");
            }
        }

        [Test]
        public void GetAverageColorIgnoresTransparentPixels()
        {
            List<Color> pixels = new() { new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 0f) };

            Color result = pixels.GetAverageColor(ColorAveragingMethod.Weighted, 0.1f);
            Assert.AreEqual(1f, result.r, 1e-3f);
            Assert.AreEqual(0f, result.g, 1e-3f);
            Assert.AreEqual(0f, result.b, 1e-3f);
        }

        [Test]
        public void GetComplementProducesOppositeHue()
        {
            Color complement = Color.red.GetComplement();
            Assert.Greater(complement.g, 0.5f);
            Assert.Greater(complement.b, 0.5f);
            Assert.Less(complement.r, 0.5f);
        }
    }
}
