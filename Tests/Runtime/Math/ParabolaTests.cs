// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Math
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Math;

    [TestFixture]
    public sealed class ParabolaTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ConstructorWithValidParametersCreatesParabola()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            Assert.AreEqual(20f, parabola.Length, Tolerance);
            Assert.AreEqual(10f, parabola.MaxHeight, Tolerance);
        }

        [Test]
        public void ConstructorWithZeroLengthThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new Parabola(maxHeight: 10f, length: 0f));
        }

        [Test]
        public void ConstructorWithNegativeLengthThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new Parabola(maxHeight: 10f, length: -5f));
        }

        [Test]
        public void ConstructorWithZeroMaxHeightThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new Parabola(maxHeight: 0f, length: 10f));
        }

        [Test]
        public void ConstructorWithNegativeMaxHeightThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new Parabola(maxHeight: -5f, length: 10f));
        }

        [Test]
        public void ConstructorCalculatesCorrectCoefficients()
        {
            // For maxHeight=10, length=20:
            // A = -4*10/400 = -0.1
            // B = 0.1*20 = 2
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            Assert.AreEqual(-0.1f, parabola.A, Tolerance);
            Assert.AreEqual(2f, parabola.B, Tolerance);
        }

        [Test]
        public void FromCoefficientsWithValidParametersCreatesParabola()
        {
            // For a parabola with intercepts at 0 and 20, max height 10:
            // A = -0.1, B = 2
            Parabola parabola = Parabola.FromCoefficients(a: -0.1f, b: 2f, length: 20f);

            Assert.AreEqual(20f, parabola.Length, Tolerance);
            Assert.AreEqual(10f, parabola.MaxHeight, Tolerance);
            Assert.AreEqual(-0.1f, parabola.A, Tolerance);
            Assert.AreEqual(2f, parabola.B, Tolerance);
        }

        [Test]
        public void FromCoefficientsWithPositiveAThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                Parabola.FromCoefficients(a: 0.1f, b: 2f, length: 20f)
            );
        }

        [Test]
        public void FromCoefficientsWithZeroAThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                Parabola.FromCoefficients(a: 0f, b: 2f, length: 20f)
            );
        }

        [Test]
        public void FromCoefficientsWithInvalidInterceptThrowsException()
        {
            // These coefficients don't produce an intercept at x=20
            Assert.Throws<ArgumentException>(() =>
                Parabola.FromCoefficients(a: -0.1f, b: 1f, length: 20f)
            );
        }

        [Test]
        public void FromCoefficientsWithNegativeLengthThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                Parabola.FromCoefficients(a: -0.1f, b: 2f, length: -20f)
            );
        }

        [Test]
        public void VertexXReturnsHalfLength()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            Assert.AreEqual(10f, parabola.VertexX, Tolerance);
        }

        [Test]
        public void VertexReturnsCorrectCoordinates()
        {
            Parabola parabola = new(maxHeight: 15f, length: 30f);
            (float x, float y) vertex = parabola.Vertex;

            Assert.AreEqual(15f, vertex.x, Tolerance);
            Assert.AreEqual(15f, vertex.y, Tolerance);
        }

        [Test]
        public void XRangeReturnsCorrectBounds()
        {
            Parabola parabola = new(maxHeight: 10f, length: 25f);
            (float min, float max) range = parabola.XRange;

            Assert.AreEqual(0f, range.min, Tolerance);
            Assert.AreEqual(25f, range.max, Tolerance);
        }

        [Test]
        public void TryGetValueAtReturnsZeroAtOrigin()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(0f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(0f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtReturnsZeroAtLength()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(20f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(0f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtReturnsMaxHeightAtVertex()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(10f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(10f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtReturnsFalseForNegativeX()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(-1f, out float y);

            Assert.IsFalse(result);
            Assert.IsTrue(float.IsNaN(y));
        }

        [Test]
        public void TryGetValueAtReturnsFalseForXBeyondLength()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(21f, out float y);

            Assert.IsFalse(result);
            Assert.IsTrue(float.IsNaN(y));
        }

        [Test]
        public void TryGetValueAtCalculatesCorrectIntermediateValues()
        {
            // For maxHeight=10, length=20: y = -0.1*x^2 + 2*x
            // At x=5: y = -0.1*25 + 10 = 7.5
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAt(5f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(7.5f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtIsSymmetricAroundVertex()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            parabola.TryGetValueAt(5f, out float y1);
            parabola.TryGetValueAt(15f, out float y2);

            Assert.AreEqual(y1, y2, Tolerance);
        }

        [Test]
        public void TryGetValueAtNormalizedAtZeroReturnsZero()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(0f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(0f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtNormalizedAtOneReturnsZero()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(1f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(0f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtNormalizedAtHalfReturnsMaxHeight()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(0.5f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(10f, y, Tolerance);
        }

        [Test]
        public void TryGetValueAtNormalizedReturnsFalseForNegativeT()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(-0.1f, out float y);

            Assert.IsFalse(result);
            Assert.IsTrue(float.IsNaN(y));
        }

        [Test]
        public void TryGetValueAtNormalizedReturnsFalseForTGreaterThanOne()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(1.1f, out float y);

            Assert.IsFalse(result);
            Assert.IsTrue(float.IsNaN(y));
        }

        [Test]
        public void TryGetValueAtNormalizedCalculatesCorrectValues()
        {
            // At t=0.25, x=5 (for length=20): y = 7.5
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            bool result = parabola.TryGetValueAtNormalized(0.25f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(7.5f, y, Tolerance);
        }

        [Test]
        public void GetValueAtUncheckedCalculatesCorrectValue()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            float y = parabola.GetValueAtUnchecked(5f);

            Assert.AreEqual(7.5f, y, Tolerance);
        }

        [Test]
        public void GetValueAtUncheckedDoesNotCheckBounds()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            // Should not throw or return NaN for out-of-bounds values
            float yNegative = parabola.GetValueAtUnchecked(-5f);
            float yBeyond = parabola.GetValueAtUnchecked(25f);

            Assert.IsFalse(float.IsNaN(yNegative));
            Assert.IsFalse(float.IsNaN(yBeyond));
        }

        [Test]
        public void EqualsReturnsTrueForSameParameters()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = new(maxHeight: 10f, length: 20f);

            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentParameters()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = new(maxHeight: 15f, length: 20f);

            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentLength()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = new(maxHeight: 10f, length: 25f);

            Assert.IsFalse(p1.Equals(p2));
        }

        [Test]
        public void EqualsWithObjectReturnsTrueForSameParabola()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            object p2 = new Parabola(maxHeight: 10f, length: 20f);

            Assert.IsTrue(p1.Equals(p2));
        }

        [Test]
        public void EqualsWithObjectReturnsFalseForNonParabola()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            object p2 = "not a parabola";

            Assert.IsFalse(p1.Equals(p2));
        }

        [Test]
        public void GetHashCodeReturnsSameValueForEqualParabolas()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = new(maxHeight: 10f, length: 20f);

            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());
        }

        [Test]
        public void GetHashCodeReturnsDifferentValueForDifferentParabolas()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = new(maxHeight: 15f, length: 20f);

            // Hash codes should typically be different (not guaranteed, but highly likely)
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());
        }

        [Test]
        public void ToStringReturnsFormattedString()
        {
            Parabola parabola = new(maxHeight: 10f, length: 20f);
            string result = parabola.ToString();

            StringAssert.Contains("maxHeight=10.00", result);
            StringAssert.Contains("length=20.00", result);
            StringAssert.Contains("vertex=(10.00, 10.00)", result);
        }

        [Test]
        public void SmallParabolaMaintainsPrecision()
        {
            Parabola parabola = new(maxHeight: 0.001f, length: 0.002f);
            bool result = parabola.TryGetValueAt(0.001f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(0.001f, y, 0.000001f);
        }

        [Test]
        public void LargeParabolaMaintainsPrecision()
        {
            Parabola parabola = new(maxHeight: 10000f, length: 20000f);
            bool result = parabola.TryGetValueAt(10000f, out float y);

            Assert.IsTrue(result);
            Assert.AreEqual(10000f, y, 1f); // Slightly higher tolerance for large values
        }

        [Test]
        public void ParabolaWithVeryDifferentScalesWorks()
        {
            Parabola parabola = new(maxHeight: 0.1f, length: 1000f);

            parabola.TryGetValueAt(0f, out float y0);
            parabola.TryGetValueAt(500f, out float yVertex);
            parabola.TryGetValueAt(1000f, out float yEnd);

            Assert.AreEqual(0f, y0, Tolerance);
            Assert.AreEqual(0.1f, yVertex, 0.001f);
            Assert.AreEqual(0f, yEnd, Tolerance);
        }

        [Test]
        public void ParabolaIsSymmetricAroundVertex()
        {
            Parabola parabola = new(maxHeight: 12f, length: 30f);

            for (float offset = 1f; offset <= 10f; offset += 1f)
            {
                float x1 = 15f - offset;
                float x2 = 15f + offset;

                parabola.TryGetValueAt(x1, out float y1);
                parabola.TryGetValueAt(x2, out float y2);

                Assert.AreEqual(y1, y2, Tolerance, $"Values at x={x1} and x={x2} should be equal");
            }
        }

        [Test]
        public void ParabolaInterceptsAreAtExpectedLocations()
        {
            Parabola parabola = new(maxHeight: 8f, length: 16f);

            parabola.TryGetValueAt(0f, out float y0);
            parabola.TryGetValueAt(16f, out float yEnd);

            Assert.AreEqual(0f, y0, Tolerance, "Y should be 0 at x=0");
            Assert.AreEqual(0f, yEnd, Tolerance, "Y should be 0 at x=Length");
        }

        [Test]
        public void ParabolaVertexIsAtExpectedLocation()
        {
            Parabola parabola = new(maxHeight: 25f, length: 50f);

            float vertexX = parabola.VertexX;
            parabola.TryGetValueAt(vertexX, out float vertexY);

            Assert.AreEqual(25f, vertexX, Tolerance);
            Assert.AreEqual(25f, vertexY, Tolerance);
        }

        [Test]
        public void ParabolaDerivativeAtVertexIsZero()
        {
            // Derivative: dy/dx = 2Ax + B
            // At vertex x = Length/2, derivative should be 0
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            float vertexX = parabola.VertexX;
            float derivative = 2f * parabola.A * vertexX + parabola.B;

            Assert.AreEqual(0f, derivative, Tolerance);
        }

        [Test]
        public void ParabolaHasNegativeSecondDerivative()
        {
            // Second derivative: d²y/dx² = 2A
            // For downward parabola, this should be negative
            Parabola parabola = new(maxHeight: 10f, length: 20f);

            float secondDerivative = 2f * parabola.A;

            Assert.Less(secondDerivative, 0f);
        }

        [Test]
        public void FromCoefficientsAndConstructorProduceSameParabola()
        {
            Parabola p1 = new(maxHeight: 10f, length: 20f);
            Parabola p2 = Parabola.FromCoefficients(p1.A, p1.B, p1.Length);

            Assert.AreEqual(p1.Length, p2.Length, Tolerance);
            Assert.AreEqual(p1.MaxHeight, p2.MaxHeight, Tolerance);
            Assert.AreEqual(p1.A, p2.A, Tolerance);
            Assert.AreEqual(p1.B, p2.B, Tolerance);
        }
    }
}
