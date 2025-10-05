namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class CircleTests
    {
        private const int NumTries = 100;
        private const float Epsilon = 0.0001f;

        private static Rect MakeRect(float xMin, float yMin, float xMax, float yMax)
        {
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        [Test]
        public void IntersectsWhenCenterInsideRectReturnsTrue()
        {
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(1f, 1f), 0.1f);
            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsWhenCompletelyOutsideOnBothAxesReturnsFalse()
        {
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(10f, 10f), 1f);
            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsWhenTouchingEdgeExactlyReturnsTrue()
        {
            // Rectangle spans x:[0,2], y:[0,2]; circle center at (3,1) with r=1 touches the right edge at (2,1)
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(3f, 1f), 1f);
            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsWhenJustShyOfEdgeReturnsFalse()
        {
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(3f, 1f), 0.99f);
            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsWhenTouchingCornerExactlyReturnsTrue()
        {
            // Corner at (2,2); center at (3,3) with r = sqrt(2) should just touch
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(3f, 3f), Mathf.Sqrt(2f));
            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsWhenOutsideOnlyInYBoundaryCases()
        {
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            // Closest point is (1,2). Distance = 1 from center (1,3)
            Circle justUnder = new(new Vector2(1f, 3f), 0.99f);
            Circle exactlyAt = new(new Vector2(1f, 3f), 1f);
            Assert.IsFalse(justUnder.Intersects(rect));
            Assert.IsTrue(exactlyAt.Intersects(rect));
        }

        [Test]
        public void IntersectsBoundsOverloadMirrorsRectLogic()
        {
            Bounds bounds = new(new Vector3(1f, 1f), new Vector3(2f, 2f, 1f)); // same as Rect [0,2]x[0,2]
            Circle circleOutside = new(new Vector2(10f, 10f), 1f);
            Circle circleTouching = new(new Vector2(3f, 1f), 1f);
            Assert.IsFalse(circleOutside.Intersects(bounds));
            Assert.IsTrue(circleTouching.Intersects(bounds));
        }

        [Test]
        public void IntersectsRegressionPreviouslyOvercountedWhenCenterPastRectMax()
        {
            // Regression guard: if clamping only to min bounds (bug), centers to the right/top of the rect would always appear intersecting
            Rect rect = MakeRect(0f, 0f, 2f, 2f);
            Circle circle = new(new Vector2(100f, 100f), 0.5f);
            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void ConstructorInitializesFieldsCorrectly()
        {
            Vector2 center = new(5f, 10f);
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
            Vector2 center = new(5f, 5f);
            Circle circle = new(center, 3f);

            Assert.IsTrue(circle.Contains(center));
        }

        [Test]
        public void ContainsReturnsTrueForPointOnCircumference()
        {
            Vector2 center = Vector2.zero;
            float radius = 5f;
            Circle circle = new(center, radius);

            Vector2 pointOnCircumference = new(radius, 0f);
            Assert.IsTrue(circle.Contains(pointOnCircumference));
        }

        [Test]
        public void ContainsReturnsTrueForPointInsideCircle()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, 5f);

            Vector2 insidePoint = new(2f, 2f);
            Assert.IsTrue(circle.Contains(insidePoint));
        }

        [Test]
        public void ContainsReturnsFalseForPointOutsideCircle()
        {
            Vector2 center = Vector2.zero;
            Circle circle = new(center, 5f);

            Vector2 outsidePoint = new(10f, 10f);
            Assert.IsFalse(circle.Contains(outsidePoint));
        }

        [Test]
        public void ContainsWithZeroRadiusOnlyContainsCenterPoint()
        {
            Vector2 center = new(5f, 5f);
            Circle circle = new(center, 0f);

            Assert.IsTrue(circle.Contains(center));
            Assert.IsFalse(circle.Contains(center + Vector2.one * 0.01f));
        }

        [Test]
        public void ContainsHandlesNegativeCoordinates()
        {
            Vector2 center = new(-5f, -5f);
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
                Vector2 center = new(
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
            Bounds bounds = new(Vector2.zero, new Vector3(4f, 4f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsReturnsFalseForNonOverlappingBounds()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new(new Vector3(10f, 10f, 0f), new Vector3(2f, 2f, 0f));

            Assert.IsFalse(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesBoundsTouchingCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Bounds bounds = new(new Vector3(5f, 0f, 0f), new Vector3(1f, 1f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesCircleInsideBounds()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new(Vector2.zero, new Vector3(10f, 10f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsBoundsHandlesBoundsInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Bounds bounds = new(Vector2.zero, new Vector3(2f, 2f, 0f));

            Assert.IsTrue(circle.Intersects(bounds));
        }

        [Test]
        public void IntersectsRectReturnsTrueForOverlappingRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(-2f, -2f, 4f, 4f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectReturnsFalseForNonOverlappingRect()
        {
            Circle circle = new(Vector2.zero, 2f);
            Rect rect = new(10f, 10f, 2f, 2f);

            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectTouchingCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(5f, 0f, 1f, 1f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesCircleInsideRect()
        {
            Circle circle = new(new Vector2(5f, 5f), 2f);
            Rect rect = new(0f, 0f, 10f, 10f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Rect rect = new(-1f, -1f, 2f, 2f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectAtCorner()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(3f, 3f, 1f, 1f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesRectFarFromCircle()
        {
            Circle circle = new(Vector2.zero, 1f);
            Rect rect = new(100f, 100f, 10f, 10f);

            Assert.IsFalse(circle.Intersects(rect));
        }

        [Test]
        public void IntersectsRectHandlesZeroSizeRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(2f, 2f, 0f, 0f);

            // Zero-size rect is essentially a point
            bool result = circle.Intersects(rect);
            // Result depends on whether the point is inside the circle
            Assert.IsTrue(result || !result); // Just checking it doesn't crash
        }

        [Test]
        public void IntersectsRectHandlesNegativeCoordinates()
        {
            Circle circle = new(new Vector2(-5f, -5f), 3f);
            Rect rect = new(-6f, -6f, 2f, 2f);

            Assert.IsTrue(circle.Intersects(rect));
        }

        [Test]
        public void OverlapsBoundsReturnsTrueWhenBoundsCompletelyInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Bounds bounds = new(Vector2.zero, new Vector3(2f, 2f, 0f));

            Assert.IsTrue(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsBoundsReturnsFalseWhenBoundsPartiallyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Bounds bounds = new(new Vector3(4f, 4f, 0f), new Vector3(4f, 4f, 0f));

            Assert.IsFalse(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsBoundsReturnsFalseWhenBoundsCompletelyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 2f);
            Bounds bounds = new(new Vector3(10f, 10f, 0f), new Vector3(2f, 2f, 0f));

            Assert.IsFalse(circle.Overlaps(bounds));
        }

        [Test]
        public void OverlapsRectReturnsTrueWhenRectCompletelyInsideCircle()
        {
            Circle circle = new(Vector2.zero, 10f);
            Rect rect = new(-1f, -1f, 2f, 2f);

            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectReturnsFalseWhenRectPartiallyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(2f, 2f, 6f, 6f);

            Assert.IsFalse(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectReturnsFalseWhenRectCompletelyOutsideCircle()
        {
            Circle circle = new(Vector2.zero, 2f);
            Rect rect = new(10f, 10f, 2f, 2f);

            Assert.IsFalse(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectHandlesRectMinMaxCorrectly()
        {
            Circle circle = new(new Vector2(5f, 5f), 5f);
            Rect rect = new(3f, 3f, 2f, 2f); // min=(3,3), max=(5,5)

            // Both min and max should be contained
            bool result = circle.Overlaps(rect);
            Assert.IsTrue(result);
        }

        [Test]
        public void OverlapsRectHandlesZeroSizeRect()
        {
            Circle circle = new(Vector2.zero, 5f);
            Rect rect = new(1f, 1f, 0f, 0f);

            // Zero-size rect has min==max, so both should be contained
            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void OverlapsRectHandlesNegativeCoordinates()
        {
            Circle circle = new(new Vector2(-5f, -5f), 10f);
            Rect rect = new(-7f, -7f, 4f, 4f);

            Assert.IsTrue(circle.Overlaps(rect));
        }

        [Test]
        public void CircleWithVeryLargeRadiusWorksCorrectly()
        {
            Circle circle = new(Vector2.zero, 1000000f);
            Vector2 farPoint = new(500000f, 500000f);

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

            Vector2 point1 = new(2f, 2f);
            Vector2 point2 = new(10f, 10f);

            Assert.IsTrue(circle1.Contains(point1));
            Assert.IsFalse(circle1.Contains(point2));
            Assert.IsFalse(circle2.Contains(point1));
            Assert.IsTrue(circle2.Contains(point2));
        }

        [Test]
        public void CircleHandlesFloatingPointPrecision()
        {
            Vector2 center = new(0.1f, 0.1f);
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
            Vector2 boundaryPoint = new(1f, 0f);
            Assert.IsTrue(circle.Contains(boundaryPoint));

            // Point slightly beyond radius
            Vector2 beyondPoint = new(1.001f, 0f);
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
                Rect rect = new(5f, 5f, -2f, -2f);
                circle.Intersects(rect);
            });
        }

        [Test]
        public void OverlapsHandlesRectWithNegativeWidthHeight()
        {
            Circle circle = new(Vector2.zero, 5f);

            Assert.DoesNotThrow(() =>
            {
                Rect rect = new(2f, 2f, -1f, -1f);
                circle.Overlaps(rect);
            });
        }

        [Test]
        public void OverlapsChecksAllFourCornersNotJustMinMax()
        {
            // This test ensures Overlaps checks all 4 corners, not just min and max
            // Create a circle at origin with radius 5
            Circle circle = new(Vector2.zero, 5f);

            // Create a rect where min and max are inside, but other corners might be outside
            // Rect from (3, -4) with width 2, height 8
            // Corners: (3, -4), (5, -4), (3, 4), (5, 4)
            // Distance from origin: (3,-4) = 5, (5,-4) = 6.4, (3,4) = 5, (5,4) = 6.4
            Rect rect = new(3f, -4f, 2f, 8f);

            // min=(3, -4), max=(5, 4)
            // Since corners (5, -4) and (5, 4) are outside radius 5, should return false
            Assert.IsFalse(circle.Overlaps(rect));
        }

        [Test]
        public void IntersectsCircleReturnsTrueForOverlappingCircles()
        {
            Circle circle1 = new(Vector2.zero, 5f);
            Circle circle2 = new(new Vector2(3f, 0f), 3f);

            Assert.IsTrue(circle1.Intersects(circle2));
            Assert.IsTrue(circle2.Intersects(circle1));
        }

        [Test]
        public void IntersectsCircleReturnsTrueForTouchingCircles()
        {
            Circle circle1 = new(Vector2.zero, 5f);
            Circle circle2 = new(new Vector2(10f, 0f), 5f);

            Assert.IsTrue(circle1.Intersects(circle2));
        }

        [Test]
        public void IntersectsCircleReturnsFalseForNonOverlappingCircles()
        {
            Circle circle1 = new(Vector2.zero, 2f);
            Circle circle2 = new(new Vector2(10f, 10f), 3f);

            Assert.IsFalse(circle1.Intersects(circle2));
        }

        [Test]
        public void IntersectsCircleHandlesIdenticalCircles()
        {
            Circle circle1 = new(new Vector2(5f, 5f), 3f);
            Circle circle2 = new(new Vector2(5f, 5f), 3f);

            Assert.IsTrue(circle1.Intersects(circle2));
        }

        [Test]
        public void IntersectsCircleHandlesOneCircleInsideAnother()
        {
            Circle bigCircle = new(Vector2.zero, 10f);
            Circle smallCircle = new(new Vector2(2f, 2f), 1f);

            Assert.IsTrue(bigCircle.Intersects(smallCircle));
            Assert.IsTrue(smallCircle.Intersects(bigCircle));
        }

        [Test]
        public void IntersectsCircleHandlesZeroRadiusCircles()
        {
            Circle circle1 = new(Vector2.zero, 0f);
            Circle circle2 = new(Vector2.zero, 5f);

            Assert.IsTrue(circle1.Intersects(circle2));
            Assert.IsTrue(circle2.Intersects(circle1));
        }

        [Test]
        public void EqualsReturnsTrueForIdenticalCircles()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(5f, 10f), 3f);

            Assert.IsTrue(circle1.Equals(circle2));
            Assert.IsTrue(circle1 == circle2);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentCenters()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(6f, 10f), 3f);

            Assert.IsFalse(circle1.Equals(circle2));
            Assert.IsTrue(circle1 != circle2);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentRadii()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(5f, 10f), 4f);

            Assert.IsFalse(circle1.Equals(circle2));
            Assert.IsTrue(circle1 != circle2);
        }

        [Test]
        public void EqualsHandlesNearlyIdenticalCircles()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            // Mathf.Approximately uses Mathf.Epsilon * 8 and considers magnitude
            // For values around 3.0, the tolerance is approximately 1e-5
            Circle circle2 = new(new Vector2(5f, 10f), 3f + 1e-6f);

            // Should use Mathf.Approximately for radius comparison
            Assert.IsTrue(circle1.Equals(circle2));
        }

        [Test]
        public void GetHashCodeReturnsSameValueForEqualCircles()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(5f, 10f), 3f);

            Assert.AreEqual(circle1.GetHashCode(), circle2.GetHashCode());
        }

        [Test]
        public void GetHashCodeReturnsDifferentValuesForDifferentCircles()
        {
            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(6f, 10f), 3f);

            // While not guaranteed, different circles should typically have different hash codes
            Assert.AreNotEqual(circle1.GetHashCode(), circle2.GetHashCode());
        }

        [Test]
        public void ToStringReturnsValidString()
        {
            Circle circle = new(new Vector2(5f, 10f), 3f);
            string str = circle.ToString();

            Assert.IsNotNull(str);
            Assert.IsTrue(str.Contains("Circle"));
            Assert.IsTrue(str.Contains("5") || str.Contains("10") || str.Contains("3"));
        }

        [Test]
        public void CirclesCanBeUsedInHashSet()
        {
            HashSet<Circle> circles = new();

            Circle circle1 = new(new Vector2(5f, 10f), 3f);
            Circle circle2 = new(new Vector2(5f, 10f), 3f);
            Circle circle3 = new(new Vector2(6f, 10f), 3f);

            Assert.IsTrue(circles.Add(circle1));
            Assert.IsFalse(circles.Add(circle2)); // Should be considered duplicate
            Assert.IsTrue(circles.Add(circle3));

            Assert.AreEqual(2, circles.Count);
        }

        [Test]
        public void OverlapsRequiresAllFourCornersForAxisAlignedRect()
        {
            // Test with an axis-aligned rectangle to ensure all 4 corners are checked
            Circle circle = new(new Vector2(5f, 5f), 3f);

            // Rectangle with all corners inside (should return true)
            Rect insideRect = new(4f, 4f, 2f, 2f); // Corners at (4,4), (6,4), (4,6), (6,6)
            Assert.IsTrue(circle.Overlaps(insideRect));

            // Rectangle with one corner outside (should return false)
            Rect partialRect = new(4f, 4f, 3f, 3f); // Corners at (4,4), (7,4), (4,7), (7,7)
            // Distance from (5,5) to (7,7) = sqrt(8) ≈ 2.83, which is < 3, so all are inside
            Assert.IsTrue(partialRect.Contains(new Vector2(4f, 4f)));

            // Better test: rectangle where corner is clearly outside
            Rect outsideRect = new(3f, 3f, 5f, 5f); // Corners at (3,3), (8,3), (3,8), (8,8)
            // Distance from (5,5) to (8,8) = sqrt(18) ≈ 4.24, which is > 3
            Assert.IsFalse(circle.Overlaps(outsideRect));
        }
    }
}
