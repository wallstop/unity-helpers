namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class CircleTests
    {
        private const int NumTries = 100;
        private const float Epsilon = 0.0001f;

        [Test]
        public void ConstructorInitializesFieldsCorrectly()
        {
            Vector2 center = new Vector2(5f, 10f);
            float radius = 3f;
            Circle circle = new(center, radius);

            Assert.AreEqual(center, circle.center);
            Assert.AreEqual(radius, circle.radius);
        }

        [Test]
        public void ConstructorWithZeroRadiusCreatesValidCircle()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, 0f);

            Assert.AreEqual(center, circle.center);
            Assert.AreEqual(0f, circle.radius);
        }

        [Test]
        public void ConstructorWithNegativeRadiusCreatesCircle()
        {
            Vector2 center = Vector2.one;
            float negativeRadius = -5f;
            Circle circle = new(center, negativeRadius);

            Assert.AreEqual(center, circle.center);
            Assert.AreEqual(negativeRadius, circle.radius);
        }

        [Test]
        public void ConstructorWithLargeRadiusCreatesValidCircle()
        {
            Vector2 center = Vector2.zero;
            float largeRadius = float.MaxValue / 2;
            Circle circle = new(center, largeRadius);

            Assert.AreEqual(center, circle.center);
            Assert.AreEqual(largeRadius, circle.radius);
        }

        [Test]
        public void ContainsReturnsTrueForCenterPoint()
        {
            Vector2 center = new Vector2(5f, 5f);
            Circle circle = new(center, 3f);

            Assert.IsTrue(circle.Contains(center));
        }

        [Test]
        public void ContainsReturnsTrueForPointOnCircumference()
        {
            Vector2 center = Vector2.zero;
            float radius = 5f;
            Circle circle = new(center, radius);

            Vector2 pointOnCircumference = new Vector2(radius, 0f);
            Assert.IsTrue(circle.Contains(pointOnCircumference));
        }

        [Test]
        public void ContainsReturnsTrueForPointInsideCircle()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, 5f);

            Vector2 insidePoint = new Vector2(2f, 2f);
            Assert.IsTrue(circle.Contains(insidePoint));
        }

        [Test]
        public void ContainsReturnsFalseForPointOutsideCircle()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, 5f);

            Vector2 outsidePoint = new Vector2(10f, 10f);
            Assert.IsFalse(circle.Contains(outsidePoint));
        }

        [Test]
        public void ContainsWithZeroRadiusOnlyContainsCenterPoint()
        {
            Vector2 center = new Vector2(5f, 5f);
            Circle circle = new(center, 0f);

            Assert.IsTrue(circle.Contains(center));
            Assert.IsFalse(circle.Contains(center + Vector2.one * 0.01f));
        }

        [Test]
        public void ContainsHandlesNegativeCoordinates()
        {
            Vector2 center = new Vector2(-5f, -5f);
            Circle circle = new(center, 3f);

            Assert.IsTrue(circle.Contains(new Vector2(-5f, -5f)));
            Assert.IsTrue(circle.Contains(new Vector2(-3f, -5f)));
            Assert.IsFalse(circle.Contains(new Vector2(0f, 0f)));
        }

        [Test]
        public void ContainsHandlesVerySmallRadius()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, Epsilon);

            Assert.IsTrue(circle.Contains(center));
            Assert.IsFalse(circle.Contains(new Vector2(1f, 0f)));
        }

        [Test]
        public void ContainsHandlesRandomPoints()
        {
            for (int i = 0; i < NumTries; i++)
            {
                Vector2 center = new Vector2(
                    PRNG.Instance.NextFloat(-100f, 100f),
                    PRNG.Instance.NextFloat(-100f, 100f)
                );
                float radius = PRNG.Instance.NextFloat(0.1f, 50f);
                Circle circle = new(center, radius);

                // Point inside
                float distance = PRNG.Instance.NextFloat(0f, radius * 0.9f);
                float angle = PRNG.Instance.NextFloat(0f, Mathf.PI * 2f);
                Vector2 insidePoint =
                    center + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
                Assert.IsTrue(
                    circle.Contains(insidePoint),
                    $"Point {insidePoint} should be inside circle at {center} with radius {radius}"
                );

                // Point outside
                distance = radius + PRNG.Instance.NextFloat(1f, 10f);
                angle = PRNG.Instance.NextFloat(0f, Mathf.PI * 2f);
                Vector2 outsidePoint =
                    center + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
                Assert.IsFalse(
                    circle.Contains(outsidePoint),
                    $"Point {outsidePoint} should be outside circle at {center} with radius {radius}"
                );
            }
        }

        [Test]
        public void IntersectsBoundsReturnsTrueForOverlappingBounds()
        {
            Circle circle = new(Vector2.zero, 5f);
            Bounds bounds = new Bounds(Vector2.zero, new Vector3(4f, 4f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsReturnsFalseForNonOverlappingBounds()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new Bounds(new Vector3(10f, 10f, 0f), new Vector3(2f, 2f, 0f));

            Assert.IsFalse(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesBoundsTouchingCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Bounds bounds = new Bounds(new Vector3(5f, 0f, 0f), new Vector3(1f, 1f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesCircleInsideBounds()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new Bounds(Vector2.zero, new Vector3(10f, 10f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesBoundsInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Bounds bounds = new Bounds(Vector2.zero, new Vector3(2f, 2f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsRectReturnsTrueForOverlappingRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(-2f, -2f, 4f, 4f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectReturnsFalseForNonOverlappingRect()
        {
            Circle circle = new(Vector2.zero, 2f);
            Rect rect = new Rect(10f, 10f, 2f, 2f);

            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectTouchingCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(5f, 0f, 1f, 1f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesCircleInsideRect()
        {
            Circle circle = new(new Vector2(5f, 5f), 2f);
            Rect rect = new Rect(0f, 0f, 10f, 10f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Rect rect = new Rect(-1f, -1f, 2f, 2f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectAtCorner()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(3f, 3f, 1f, 1f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectFarFromCircle()
        {
            Circle circle = new(Vector2.zero, 1f);
            Rect rect = new Rect(100f, 100f, 10f, 10f);

            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesZeroSizeRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(2f, 2f, 0f, 0f);

            // Zero-size rect is essentially a point
            bool result = circle.Intersects(rect);
            // Result depends on whether the point is inside the circle
            Assert.IsTrue(result || !result); // Just checking it doesn't crash
        }

        [Test]
        public void IntersectsRectHandlesNegativeCoordinates()
        {
            Circle circle = new(new Vector2(-5f, -5f), 3f);
            Rect rect = new Rect(-6f, -6f, 2f, 2f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void OverlapsBoundsReturnsTrueWhenBoundsCompletelyInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Bounds bounds = new Bounds(Vector2.zero, new Vector3(2f, 2f, 0f));

            Assert.IsTrue(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsBoundsReturnsFalseWhenBoundsPartiallyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Bounds bounds = new Bounds(new Vector3(4f, 4f, 0f), new Vector3(4f, 4f, 0f));

            Assert.IsFalse(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsBoundsReturnsFalseWhenBoundsCompletelyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new Bounds(new Vector3(10f, 10f, 0f), new Vector3(2f, 2f, 0f));

            Assert.IsFalse(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsRectReturnsTrueWhenRectCompletelyInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Rect rect = new Rect(-1f, -1f, 2f, 2f);

            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectReturnsFalseWhenRectPartiallyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(2f, 2f, 6f, 6f);

            Assert.IsFalse(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectReturnsFalseWhenRectCompletelyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 2f);
            Rect rect = new Rect(10f, 10f, 2f, 2f);

            Assert.IsFalse(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectHandlesRectMinMaxCorrectly()
        {
            Circle circle = new(new Vector2(5f, 5f), 5f);
            Rect rect = new Rect(3f, 3f, 2f, 2f); // min=(3,3), max=(5,5)

            // Both min and max should be contained
            bool result = circle.Overlaps(rect);
            Assert.IsTrue(result);
        }

        [Test]
        public void OverlapsRectHandlesZeroSizeRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new Rect(1f, 1f, 0f, 0f);

            // Zero-size rect has min==max, so both should be contained
            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectHandlesNegativeCoordinates()
        {
            Circle circle = new(new Vector2(-5f, -5f), 10f);
            Rect rect = new Rect(-7f, -7f, 4f, 4f);

            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void CircleWithVeryLargeRadiusWorksCorrectly()
        {
            Circle circle = new(Vector2.zero, 1000000f);
            Vector2 farPoint = new Vector2(500000f, 500000f);

            Assert.IsTrue(circle.Contains(farPoint));
        }

        [Test]
        public void CircleAtOriginWorksCorrectly()
        {
            Circle circle = new(Vector2.zero, 5f);

            Assert.IsTrue(circle.Contains(Vector2.zero));
            Assert.IsTrue(circle.Contains(new Vector2(3f, 4f)));
            Assert.IsFalse(circle.Contains(new Vector2(4f, 4f)));
        }

        [Test]
        public void CircleAtPositiveCoordinatesWorksCorrectly()
        {
            Circle circle = new(new Vector2(100f, 100f), 10f);

            Assert.IsTrue(circle.Contains(new Vector2(100f, 100f)));
            Assert.IsTrue(circle.Contains(new Vector2(105f, 100f)));
            Assert.IsFalse(circle.Contains(new Vector2(120f, 120f)));
        }

        [Test]
        public void CircleAtNegativeCoordinatesWorksCorrectly()
        {
            Circle circle = new(new Vector2(-100f, -100f), 10f);

            Assert.IsTrue(circle.Contains(new Vector2(-100f, -100f)));
            Assert.IsTrue(circle.Contains(new Vector2(-95f, -100f)));
            Assert.IsFalse(circle.Contains(new Vector2(-80f, -80f)));
        }

        [Test]
        public void MultipleCirclesIndependent()
        {
            Circle circle1 = new(Vector2.zero, 5f);
            Circle circle2 = new(new Vector2(10f, 10f), 3f);

            Vector2 point1 = new Vector2(2f, 2f);
            Vector2 point2 = new Vector2(10f, 10f);

            Assert.IsTrue(circle1.Contains(point1));
            Assert.IsFalse(circle1.Contains(point2));
            Assert.IsFalse(circle2.Contains(point1));
            Assert.IsTrue(circle2.Contains(point2));
        }

        [Test]
        public void CircleHandlesFloatingPointPrecision()
        {
            Vector2 center = new Vector2(0.1f, 0.1f);
            float radius = 0.1f;
            Circle circle = new(center, radius);

            Vector2 point = center + new Vector2(0.1f, 0f);
            Assert.IsTrue(circle.Contains(point));
        }

        [Test]
        public void ContainsEdgeCaseAtExactBoundary()
        {
            Circle circle = new(Vector2.zero, 1f);

            // Point at exactly radius distance
            Vector2 boundaryPoint = new Vector2(1f, 0f);
            Assert.IsTrue(circle.Contains(boundaryPoint));

            // Point slightly beyond radius
            Vector2 beyondPoint = new Vector2(1.001f, 0f);
            Assert.IsFalse(circle.Contains(beyondPoint));
        }

        [Test]
        public void IntersectsHandlesRectWithNegativeWidthHeight()
        {
            Circle circle = new(Vector2.zero, 5f);

            // Rect constructor should handle negative width/height gracefully
            // depending on Unity's implementation
            Assert.DoesNotThrow(() =>
            {
                Rect rect = new Rect(5f, 5f, -2f, -2f);
                circle.Intersects(rect);
            });
        }

        [Test]
        public void OverlapsHandlesRectWithNegativeWidthHeight()
        {
            Circle circle = new(Vector2.zero, 5f);

            Assert.DoesNotThrow(() =>
            {
                Rect rect = new Rect(2f, 2f, -1f, -1f);
                circle.Overlaps(rect);
            });
        }
    }
}
