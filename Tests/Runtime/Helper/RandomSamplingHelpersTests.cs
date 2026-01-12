// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class RandomSamplingHelpersTests
    {
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-5d)]
        [TestCase(0d)]
        public void ClampUnitIntervalDoubleReturnsZeroForInvalidInputs(double sample)
        {
            Assert.That(Helpers.ClampUnitInterval(sample), Is.EqualTo(0d));
        }

        [Test]
        public void ClampUnitIntervalDoubleCapsUpperBound()
        {
            double expected = BitConverter.Int64BitsToDouble(
                BitConverter.DoubleToInt64Bits(1d) - 1L
            );
            double sanitized = Helpers.ClampUnitInterval(1d + 1e-6);
            Assert.That(sanitized, Is.EqualTo(expected));
        }

        [Test]
        public void ClampUnitIntervalDoublePassesThroughValidValue()
        {
            const double sample = 0.42d;
            Assert.That(Helpers.ClampUnitInterval(sample), Is.EqualTo(sample));
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-2f)]
        [TestCase(0f)]
        public void ClampUnitIntervalFloatReturnsZeroForInvalidInputs(float sample)
        {
            Assert.That(Helpers.ClampUnitInterval(sample), Is.EqualTo(0f));
        }

        [Test]
        public void ClampUnitIntervalFloatCapsUpperBound()
        {
            float expected = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) - 1);
            float sanitized = Helpers.ClampUnitInterval(10f);
            Assert.That(sanitized, Is.EqualTo(expected));
        }

        [Test]
        public void ClampUnitIntervalFloatPassesThroughValidValue()
        {
            const float sample = 0.1337f;
            Assert.That(Helpers.ClampUnitInterval(sample), Is.EqualTo(sample));
        }
    }
}
