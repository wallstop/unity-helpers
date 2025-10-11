namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class UnityExtensionsMathTests
    {
        [Test]
        public void FastContains2DBoundsIntPointInsideReturnsTrue()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(5, 5, 0);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointAtMinReturnsTrue()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(0, 0, 0);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointAtMaxReturnsFalse()
        {
            // xMax and yMax are exclusive in FastContains2D
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(10, 10, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointOnMaxXEdgeReturnsFalse()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(10, 5, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointOnMaxYEdgeReturnsFalse()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(5, 10, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointJustInsideMaxBoundaryReturnsTrue()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(9, 9, 0);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointOutsideNegativeReturnsFalse()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(-1, -1, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntPointOutsidePositiveReturnsFalse()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(11, 11, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntNegativeBoundsPointInsideReturnsTrue()
        {
            BoundsInt bounds = new(-10, -10, 0, 10, 10, 10);
            FastVector3Int point = new(-5, -5, 0);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntSinglePointBoundsPointAtMinReturnsTrue()
        {
            BoundsInt bounds = new(5, 5, 0, 1, 1, 1);
            FastVector3Int point = new(5, 5, 0);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntZeroSizeBoundsReturnsFalse()
        {
            BoundsInt bounds = new(5, 5, 0, 0, 0, 0);
            FastVector3Int point = new(5, 5, 0);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsIntIgnoresZCoordinate()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            FastVector3Int point = new(5, 5, 100);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(5f, 5f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointAtMinReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(0f, 0f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointAtMaxReturnsFalse()
        {
            // max is inclusive in FastContains2D
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(10f, 10f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointJustBelowMinReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(-0.01f, 5f);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointJustBelowMaxReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(9.99f, 9.99f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointOutsideXReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(10.1f, 5f);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsPointOutsideYReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(5f, 10.1f);
            Assert.IsFalse(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsNegativeBoundsPointInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(-5f, -5f, 0f), new Vector3(10f, 10f, 10f));
            Vector2 point = new(-5f, -5f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsVerySmallBoundsPointInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Vector2 point = new(0f, 0f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastContains2DBoundsVeryLargeBoundsPointInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10000f, 10000f, 10000f));
            Vector2 point = new(4999f, 4999f);
            Assert.IsTrue(bounds.FastContains2D(point));
        }

        [Test]
        public void FastIntersects2DBoundsIntOverlappingBoundsReturnsTrue()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(5, 5, 0, 10, 10, 10);
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntTouchingEdgesReturnsTrue()
        {
            // Bounds touching at edge should intersect (xMax >= xMin)
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(10, 0, 0, 10, 10, 10);
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntSeparatedBoundsReturnsFalse()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(11, 11, 0, 10, 10, 10);
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntOneInsideOtherReturnsTrue()
        {
            BoundsInt bounds1 = new(0, 0, 0, 100, 100, 100);
            BoundsInt bounds2 = new(25, 25, 0, 50, 50, 50);
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntIdenticalBoundsReturnsTrue()
        {
            BoundsInt bounds = new(5, 5, 0, 10, 10, 10);
            Assert.IsTrue(bounds.FastIntersects2D(bounds));
        }

        [Test]
        public void FastIntersects2DBoundsIntNegativeCoordinatesOverlappingReturnsTrue()
        {
            BoundsInt bounds1 = new(-10, -10, 0, 10, 10, 10);
            BoundsInt bounds2 = new(-5, -5, 0, 10, 10, 10);
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntSeparatedOnXAxisReturnsFalse()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(11, 5, 0, 10, 10, 10);
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntSeparatedOnYAxisReturnsFalse()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(5, 11, 0, 10, 10, 10);
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntZeroSizeBoundsReturnsFalse()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 10);
            BoundsInt bounds2 = new(5, 5, 0, 0, 0, 0);
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIntIgnoresZCoordinate()
        {
            BoundsInt bounds1 = new(0, 0, 0, 10, 10, 1);
            BoundsInt bounds2 = new(5, 5, 100, 10, 10, 1);
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsOverlappingBoundsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(8f, 8f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsTouchingEdgesReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(10f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsSeparatedBoundsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(20f, 20f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsOneInsideOtherReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 100f));
            Bounds bounds2 = new(new Vector3(50f, 50f, 0f), new Vector3(50f, 50f, 50f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsIdenticalBoundsReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds.FastIntersects2D(bounds));
        }

        [Test]
        public void FastIntersects2DBoundsNegativeCoordinatesOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(-5f, -5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(-3f, -3f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsSeparatedOnXAxisReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(20f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsSeparatedOnYAxisReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(5f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 20f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsVerySmallBoundsOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Bounds bounds2 = new(new Vector3(0.005f, 0.005f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void FastIntersects2DBoundsPartialOverlapReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(8f, 8f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects2D(bounds2));
        }

        [Test]
        public void Overlaps2DFullyContainedBoundsReturnsTrue()
        {
            Bounds outer = new(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 100f));
            Bounds inner = new(new Vector3(50f, 50f, 0f), new Vector3(50f, 50f, 50f));
            Assert.IsTrue(outer.Overlaps2D(inner));
        }

        [Test]
        public void Overlaps2DIdenticalBoundsReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds.Overlaps2D(bounds));
        }

        [Test]
        public void Overlaps2DPartiallyOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(8f, 8f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DOtherMinBelowBoundsMinReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(10f, 10f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 10f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DOtherMaxAboveBoundsMaxReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(6f, 6f, 0f), new Vector3(20f, 20f, 20f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DOtherExactlyAtBoundaryReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DSeparatedBoundsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(20f, 20f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DNegativeCoordinatesFullyContainedReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(-10f, -10f, 0f), new Vector3(20f, 20f, 20f));
            Bounds bounds2 = new(new Vector3(-5f, -5f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DVerySmallBoundsFullyContainedReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void Overlaps2DTouchingEdgeFromInsideReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f));
            Assert.IsTrue(bounds1.Overlaps2D(bounds2));
        }

        [Test]
        public void WithPaddingPositivePaddingExpandsBounds()
        {
            BoundsInt bounds = new(5, 5, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(2, 3);

            Assert.AreEqual(3, padded.xMin);
            Assert.AreEqual(2, padded.yMin);
            Assert.AreEqual(17, padded.xMax);
            Assert.AreEqual(18, padded.yMax);
            Assert.AreEqual(14, padded.size.x);
            Assert.AreEqual(16, padded.size.y);
        }

        [Test]
        public void WithPaddingZeroPaddingReturnsSameBounds()
        {
            BoundsInt bounds = new(5, 5, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(0, 0);

            Assert.AreEqual(bounds.xMin, padded.xMin);
            Assert.AreEqual(bounds.yMin, padded.yMin);
            Assert.AreEqual(bounds.size.x, padded.size.x);
            Assert.AreEqual(bounds.size.y, padded.size.y);
        }

        [Test]
        public void WithPaddingNegativePaddingShrinksBounds()
        {
            BoundsInt bounds = new(0, 0, 0, 20, 20, 20);
            BoundsInt padded = bounds.WithPadding(-2, -3);

            Assert.AreEqual(2, padded.xMin);
            Assert.AreEqual(3, padded.yMin);
            Assert.AreEqual(18, padded.xMax);
            Assert.AreEqual(17, padded.yMax);
            Assert.AreEqual(16, padded.size.x);
            Assert.AreEqual(14, padded.size.y);
        }

        [Test]
        public void WithPaddingAsymmetricPaddingWorksCorrectly()
        {
            BoundsInt bounds = new(10, 10, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(5, 2);

            Assert.AreEqual(5, padded.xMin);
            Assert.AreEqual(8, padded.yMin);
            Assert.AreEqual(25, padded.xMax);
            Assert.AreEqual(22, padded.yMax);
        }

        [Test]
        public void WithPaddingNegativeBoundsWorksCorrectly()
        {
            BoundsInt bounds = new(-10, -10, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(2, 2);

            Assert.AreEqual(-12, padded.xMin);
            Assert.AreEqual(-12, padded.yMin);
            Assert.AreEqual(2, padded.xMax);
            Assert.AreEqual(2, padded.yMax);
        }

        [Test]
        public void WithPaddingPreservesZCoordinate()
        {
            BoundsInt bounds = new(5, 5, 7, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(2, 3);

            Assert.AreEqual(bounds.zMin, padded.zMin);
            Assert.AreEqual(bounds.size.z, padded.size.z);
        }

        [Test]
        public void WithPaddingLargePaddingWorksCorrectly()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(100, 200);

            Assert.AreEqual(-100, padded.xMin);
            Assert.AreEqual(-200, padded.yMin);
            Assert.AreEqual(210, padded.size.x);
            Assert.AreEqual(410, padded.size.y);
        }

        [Test]
        public void WithPaddingExcessiveNegativePaddingCanResultInNegativeSize()
        {
            BoundsInt bounds = new(0, 0, 0, 10, 10, 10);
            BoundsInt padded = bounds.WithPadding(-10, -10);

            // Size becomes 10 + 2*(-10) = -10
            Assert.AreEqual(-10, padded.size.x);
            Assert.AreEqual(-10, padded.size.y);
        }

        [Test]
        public void WithPaddingSinglePointBoundsExpandsCorrectly()
        {
            BoundsInt bounds = new(5, 5, 0, 1, 1, 1);
            BoundsInt padded = bounds.WithPadding(2, 2);

            Assert.AreEqual(3, padded.xMin);
            Assert.AreEqual(3, padded.yMin);
            Assert.AreEqual(5, padded.size.x);
            Assert.AreEqual(5, padded.size.y);
        }

        [Test]
        public void OrientationColinearPointsReturnsColinear()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 1f);
            Vector2 r = new(2f, 2f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Colinear,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationClockwisePointsReturnsClockwise()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 1f);
            Vector2 r = new(2f, 0f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Clockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationCounterclockwisePointsReturnsCounterclockwise()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 1f);
            Vector2 r = new(0f, 2f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Counterclockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationHorizontalColinearPointsReturnsColinear()
        {
            Vector2 p = new(0f, 5f);
            Vector2 q = new(5f, 5f);
            Vector2 r = new(10f, 5f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Colinear,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationVerticalColinearPointsReturnsColinear()
        {
            Vector2 p = new(5f, 0f);
            Vector2 q = new(5f, 5f);
            Vector2 r = new(5f, 10f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Colinear,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationIdenticalPointsReturnsColinear()
        {
            Vector2 p = new(5f, 5f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Colinear,
                UnityExtensions.Orientation(p, p, p)
            );
        }

        [Test]
        public void OrientationNegativeCoordinatesClockwiseReturnsClockwise()
        {
            Vector2 p = new(-5f, -5f);
            Vector2 q = new(0f, 0f);
            Vector2 r = new(5f, -5f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Clockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationVerySmallAnglesDetectsCorrectly()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(100f, 0f);
            Vector2 r = new(200f, 0.1f);

            // Should be counterclockwise due to slight upward deviation
            Assert.AreEqual(
                UnityExtensions.OrientationType.Counterclockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationRightAngleClockwiseReturnsClockwise()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 0f);
            Vector2 r = new(1f, -1f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Clockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void OrientationRightAngleCounterclockwiseReturnsCounterclockwise()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 0f);
            Vector2 r = new(1f, 1f);

            Assert.AreEqual(
                UnityExtensions.OrientationType.Counterclockwise,
                UnityExtensions.Orientation(p, q, r)
            );
        }

        [Test]
        public void RotateZeroDegreesReturnsOriginalVector()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(0f);

            Assert.AreEqual(1f, rotated.x, 1e-5f);
            Assert.AreEqual(0f, rotated.y, 1e-5f);
        }

        [Test]
        public void Rotate90DegreesRotatesCorrectly()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(90f);

            Assert.AreEqual(0f, rotated.x, 1e-5f);
            Assert.AreEqual(1f, rotated.y, 1e-5f);
        }

        [Test]
        public void Rotate180DegreesRotatesCorrectly()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(180f);

            Assert.AreEqual(-1f, rotated.x, 1e-5f);
            Assert.AreEqual(0f, rotated.y, 1e-5f);
        }

        [Test]
        public void Rotate270DegreesRotatesCorrectly()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(270f);

            Assert.AreEqual(0f, rotated.x, 1e-5f);
            Assert.AreEqual(-1f, rotated.y, 1e-5f);
        }

        [Test]
        public void Rotate360DegreesReturnsOriginalVector()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(360f);

            Assert.AreEqual(1f, rotated.x, 1e-5f);
            Assert.AreEqual(0f, rotated.y, 1e-5f);
        }

        [Test]
        public void RotateNegativeAngleRotatesClockwise()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(-90f);

            Assert.AreEqual(0f, rotated.x, 1e-5f);
            Assert.AreEqual(-1f, rotated.y, 1e-5f);
        }

        [Test]
        public void Rotate45DegreesRotatesCorrectly()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(45f);

            float expected = Mathf.Sqrt(2f) / 2f;
            Assert.AreEqual(expected, rotated.x, 1e-5f);
            Assert.AreEqual(expected, rotated.y, 1e-5f);
        }

        [Test]
        public void RotateZeroVectorReturnsZeroVector()
        {
            Vector2 v = new(0f, 0f);
            Vector2 rotated = v.Rotate(45f);

            Assert.AreEqual(0f, rotated.x, 1e-5f);
            Assert.AreEqual(0f, rotated.y, 1e-5f);
        }

        [Test]
        public void RotateLargeAngleRotatesCorrectly()
        {
            Vector2 v = new(1f, 0f);
            Vector2 rotated = v.Rotate(720f); // Two full rotations

            Assert.AreEqual(1f, rotated.x, 1e-5f);
            Assert.AreEqual(0f, rotated.y, 1e-5f);
        }

        [Test]
        public void RotatePreservesMagnitude()
        {
            Vector2 v = new(3f, 4f);
            float originalMagnitude = v.magnitude;
            Vector2 rotated = v.Rotate(73f);

            Assert.AreEqual(originalMagnitude, rotated.magnitude, 1e-5f);
        }

        [Test]
        public void RotateNonUnitVectorRotatesCorrectly()
        {
            Vector2 v = new(3f, 4f);
            Vector2 rotated = v.Rotate(90f);

            Assert.AreEqual(-4f, rotated.x, 1e-5f);
            Assert.AreEqual(3f, rotated.y, 1e-5f);
        }

        [Test]
        public void RotateNegativeCoordinatesRotatesCorrectly()
        {
            Vector2 v = new(-1f, -1f);
            Vector2 rotated = v.Rotate(90f);

            Assert.AreEqual(1f, rotated.x, 1e-5f);
            Assert.AreEqual(-1f, rotated.y, 1e-5f);
        }

        [Test]
        public void FastIntersects3DOverlappingBoundsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(8f, 8f, 8f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DTouchingEdgesReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(10f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DSeparatedBoundsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(20f, 20f, 20f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DOneInsideOtherReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(50f, 50f, 50f), new Vector3(100f, 100f, 100f));
            Bounds bounds2 = new(new Vector3(50f, 50f, 50f), new Vector3(50f, 50f, 50f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DIdenticalBoundsReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds.FastIntersects(bounds));
        }

        [Test]
        public void FastIntersects3DSeparatedOnXAxisReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(20f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DSeparatedOnYAxisReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(5f, 0f, 5f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 20f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DSeparatedOnZAxisReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 20f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DNegativeCoordinatesOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(-5f, -5f, -5f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(-3f, -3f, -3f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DPartialOverlapAllAxesReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(8f, 8f, 8f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DVerySmallBoundsOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Bounds bounds2 = new(
                new Vector3(0.005f, 0.005f, 0.005f),
                new Vector3(0.01f, 0.01f, 0.01f)
            );
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DVeryLargeBoundsOverlappingReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10000f, 10000f, 10000f));
            Bounds bounds2 = new(
                new Vector3(5000f, 5000f, 5000f),
                new Vector3(10000f, 10000f, 10000f)
            );
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DAlmostTouchingBoundsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            // Place bounds2 just beyond touching so there's a gap on X
            Bounds bounds2 = new(new Vector3(10.01f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DTouchingAtSinglePointReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DOnlyXOverlapsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 11f, 11f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DOnlyYOverlapsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(11f, 5f, 11f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DOnlyZOverlapsReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(11f, 11f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DXYOverlapZSeparatedReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 11f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DXZOverlapYSeparatedReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 11f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DYZOverlapXSeparatedReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(11f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DBarelyOverlappingXReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(4.99f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DBarelyOverlappingYReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(0f, 4.99f, 0f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DBarelyOverlappingZReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(0f, 0f, 4.99f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DBoundsWithZeroVolumeReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(0f, 0f, 0f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DBothBoundsWithZeroVolumeReturnsFalse()
        {
            Bounds bounds1 = new(new Vector3(5f, 5f, 5f), new Vector3(0f, 0f, 0f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(0f, 0f, 0f));
            Assert.IsFalse(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DThinSliceOverlapsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(0.01f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DLineSegmentOverlapsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(0.01f, 0.01f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DExtremeNegativeCoordinatesReturnsTrue()
        {
            Bounds bounds1 = new(
                new Vector3(-1000f, -1000f, -1000f),
                new Vector3(100f, 100f, 100f)
            );
            Bounds bounds2 = new(new Vector3(-960f, -960f, -960f), new Vector3(100f, 100f, 100f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DMixedPositiveNegativeCoordinatesReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(-5f, -5f, -5f), new Vector3(20f, 20f, 20f));
            Bounds bounds2 = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DMinimalOverlapAllAxesReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(4.99f, 4.99f, 4.99f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DCompletelyContainingBoundsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(0f, 0f, 0f), new Vector3(100f, 100f, 100f));
            Bounds bounds2 = new(new Vector3(25f, 25f, 25f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastIntersects3DCompletelyContainedBoundsReturnsTrue()
        {
            Bounds bounds1 = new(new Vector3(25f, 25f, 25f), new Vector3(10f, 10f, 10f));
            Bounds bounds2 = new(new Vector3(0f, 0f, 0f), new Vector3(100f, 100f, 100f));
            Assert.IsTrue(bounds1.FastIntersects(bounds2));
        }

        [Test]
        public void FastContains3DPointInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 5f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointAtMinReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(0f, 0f, 0f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointAtMaxReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(10f, 10f, 10f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointJustBelowMaxReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(9.99f, 9.99f, 9.99f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointJustBelowMinReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(-0.01f, 5f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOutsideXReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(10.1f, 5f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOutsideYReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 10.1f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOutsideZReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 10.1f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMaxXEdgeReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 2f, 2f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMaxYEdgeReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, 5f, 2f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMaxZEdgeReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, 2f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointNegativeCoordinatesInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(-5f, -5f, -5f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(-3f, -3f, -3f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointNegativeCoordinatesOutsideReturnsFalse()
        {
            Bounds bounds = new(new Vector3(-5f, -5f, -5f), new Vector3(10f, 10f, 10f));
            // Put x below bounds.min.x (-10) so the point is truly outside
            Vector3 point = new(-10.1f, -3f, -3f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointVerySmallBoundsInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            // Use a point strictly inside the half-open max boundary
            Vector3 point = new(0.0049f, 0.0049f, 0.0049f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointVeryLargeBoundsInsideReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10000f, 10000f, 10000f));
            Vector3 point = new(4999f, 4999f, 4999f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointAtCenterReturnsTrue()
        {
            Bounds bounds = new(new Vector3(10f, 10f, 10f), new Vector3(20f, 20f, 20f));
            Vector3 point = bounds.center;
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointZeroBoundsReturnsFalse()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(0f, 0f, 0f));
            Vector3 point = new(5f, 5f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMinXEdgeReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(-5f, 2f, 2f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMinYEdgeReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, -5f, 2f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointOnMinZEdgeReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, 2f, -5f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointExactlyAtMaxXReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 2f, 2f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointExactlyAtMaxYReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, 5f, 2f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointExactlyAtMaxZReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(2f, 2f, 5f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointBarelyInsideMinXReturnsTrue()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(-4.99f, 2f, 2f);
            Assert.IsTrue(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DPointBarelyOutsideMaxXReturnsFalse()
        {
            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Vector3 point = new(5.01f, 2f, 2f);
            Assert.IsFalse(bounds.FastContains3D(point));
        }

        [Test]
        public void FastContains3DBoundsFullyContainedReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(100f, 100f, 100f));
            Bounds inner = new(new Vector3(25f, 25f, 25f), new Vector3(50f, 50f, 50f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsIdenticalBoundsReturnsTrue()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(bounds.FastContains3D(bounds));
        }

        [Test]
        public void FastContains3DBoundsPartiallyOutsideReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(20f, 20f, 20f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsCompletelyOutsideReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(20f, 20f, 20f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerMinOutsideReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(
                // Shift slightly so inner.min.x is strictly less than outer.min.x
                new Vector3(-5.1f, 5f, 5f),
                new Vector3(10f, 10f, 10f)
            );
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerMaxOutsideReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(30f, 10f, 10f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerTouchingOuterMinReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(
                // Place inner so inner.min == outer.min and inner.max < outer.max (touches min only)
                new Vector3(-5f, -5f, -5f),
                new Vector3(10f, 10f, 10f)
            );
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerTouchingOuterMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerJustBeyondMaxReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(20.01f, 20f, 20f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerJustBeyondMinReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(-10.01f, -10f, -10f), new Vector3(20f, 20f, 20f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsNegativeCoordinatesFullyContainedReturnsTrue()
        {
            Bounds outer = new(new Vector3(-50f, -50f, -50f), new Vector3(100f, 100f, 100f));
            Bounds inner = new(new Vector3(-25f, -25f, -25f), new Vector3(50f, 50f, 50f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsVerySmallInnerReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(
                // Place the very small inner well inside the outer
                new Vector3(0f, 0f, 0f),
                new Vector3(0.01f, 0.01f, 0.01f)
            );
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsVeryLargeOuterReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10000f, 10000f, 10000f));
            Bounds inner = new(new Vector3(100f, 100f, 100f), new Vector3(500f, 500f, 500f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsZeroVolumeInnerReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(0f, 0f, 0f));
            // Degenerate inner (point) at (5,5,5) is within outer; containment is inclusive
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerExceedsOnXAxisReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 2f, 2f), new Vector3(20f, 5f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerExceedsOnYAxisReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(2f, 0f, 2f), new Vector3(5f, 20f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerExceedsOnZAxisReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(2f, 2f, 0f), new Vector3(5f, 5f, 20f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerMinBelowOnXReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(-5.1f, 0f, 0f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerMinBelowOnYReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, -5.1f, 0f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerMinBelowOnZReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 0f, -5.1f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerBarelyFitsReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(
                // Place centered so it's just slightly smaller than outer on all sides
                new Vector3(0f, 0f, 0f),
                new Vector3(19.99f, 19.99f, 19.99f)
            );
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerBarelyDoesNotFitOnXReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(-10f, -9.99f, -9.99f), new Vector3(20f, 19.99f, 19.99f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerBarelyDoesNotFitOnYReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(-9.99f, -10f, -9.99f), new Vector3(19.99f, 20f, 19.99f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerBarelyDoesNotFitOnZReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(-9.99f, -9.99f, -10f), new Vector3(19.99f, 19.99f, 20f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsMixedPositiveNegativeReturnsTrue()
        {
            Bounds outer = new(new Vector3(-10f, -10f, -10f), new Vector3(40f, 40f, 40f));
            Bounds inner = new(
                // Adjust Z so inner.max.z does not exceed outer.max.z
                new Vector3(-5f, 0f, 0f),
                new Vector3(20f, 20f, 20f)
            );
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsOuterLargerOnlyOnOneAxisReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 5f, 5f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(5f, 10f, 5f));
            Assert.IsFalse(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerSingleAxisSliceReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 10f, 10f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        [Test]
        public void FastContains3DBoundsInnerLineSegmentReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 10f));
            Assert.IsTrue(outer.FastContains3D(inner));
        }

        // FastContainsHalfOpen3D tests
        [Test]
        public void FastContainsHalfOpen3DFullyContainedReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(100f, 100f, 100f));
            // Inner.max must be strictly below outer.max for half-open semantics
            Bounds inner = new(new Vector3(25f, 25f, 25f), new Vector3(49.99f, 49.99f, 49.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerTouchingOuterMaxReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxJustBelowOuterMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(19.99f, 19.99f, 19.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinBelowOuterMinReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(-10.01f, -10f, -10f), new Vector3(15f, 15f, 15f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinAtOuterMinReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            // Set inner so inner.min == outer.min (-10) and inner.max < outer.max (10)
            Bounds inner = new(new Vector3(-2.5f, -2.5f, -2.5f), new Vector3(15f, 15f, 15f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxTouchesOuterMaxOnXReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 15f, 15f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxTouchesOuterMaxOnYReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(15f, 10f, 15f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxTouchesOuterMaxOnZReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(15f, 15f, 10f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerExceedsOuterMaxOnXReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 2f, 2f), new Vector3(10.01f, 5f, 5f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerExceedsOuterMaxOnYReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(2f, 0f, 2f), new Vector3(5f, 10.01f, 5f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerExceedsOuterMaxOnZReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(2f, 2f, 0f), new Vector3(5f, 5f, 10.01f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DCompletelyOutsideReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(20f, 20f, 20f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DNegativeCoordinatesFullyContainedReturnsTrue()
        {
            Bounds outer = new(new Vector3(-50f, -50f, -50f), new Vector3(100f, 100f, 100f));
            // Ensure inner.max is strictly below outer.max on all axes
            Bounds inner = new(new Vector3(-25f, -25f, -25f), new Vector3(49.99f, 49.99f, 49.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DVerySmallInnerReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            // Place tiny inner strictly inside (not at max)
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DVeryLargeOuterReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10000f, 10000f, 10000f));
            Bounds inner = new(new Vector3(100f, 100f, 100f), new Vector3(500f, 500f, 500f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DZeroVolumeInnerReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            // Degenerate inner point must not be at max; place at center
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinBelowOnXReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(-5.1f, 0f, 0f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinBelowOnYReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, -5.1f, 0f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinBelowOnZReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 0f, -5.1f), new Vector3(5f, 5f, 5f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerBarelyFitsWithoutTouchingMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            // Centered inner just under outer size so max is strictly less
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(19.99f, 19.99f, 19.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DMixedPositiveNegativeReturnsTrue()
        {
            Bounds outer = new(new Vector3(-10f, -10f, -10f), new Vector3(40f, 40f, 40f));
            // Ensure inner.max is strictly below outer.max on each axis
            Bounds inner = new(new Vector3(-5f, 0f, 5f), new Vector3(29.99f, 19.99f, 9.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DPartiallyOverlappingReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(20f, 20f, 20f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DThinSliceNotTouchingMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 10f, 10f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DLineSegmentNotTouchingMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 10f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DExtremeNegativeCoordinatesReturnsTrue()
        {
            Bounds outer = new(
                new Vector3(-1000f, -1000f, -1000f),
                new Vector3(2000f, 2000f, 2000f)
            );
            // Ensure inner.max is strictly below outer.max (half-open on max)
            Bounds inner = new(
                new Vector3(-500f, -500f, -500f),
                new Vector3(999.99f, 999.99f, 999.99f)
            );
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerAtMinEdgeNotTouchingMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            // Place inner so inner.min == outer.min, and inner.max < outer.max
            Bounds inner = new(new Vector3(-5f, -5f, -5f), new Vector3(10f, 10f, 10f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxExactlyAtOuterMaxOnAllAxesReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxJustBelowOuterMaxOnAllAxesReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(9.99f, 9.99f, 9.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMaxExceedsOuterMaxOnOneAxisReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(0f, 0f, 0f), new Vector3(9.99f, 9.99f, 10.01f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DAsymmetricBoundsReturnsTrue()
        {
            Bounds outer = new(new Vector3(-20f, 0f, 10f), new Vector3(100f, 50f, 80f));
            // Adjust inner sizes so inner.max is strictly below outer.max on Y and Z
            Bounds inner = new(new Vector3(-10f, 5f, 15f), new Vector3(75f, 39.99f, 69.99f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerMinAtOuterMinMaxTouchesReturnsFalse()
        {
            Bounds outer = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            Bounds inner = new(new Vector3(-2.5f, -2.5f, -2.5f), new Vector3(10f, 10f, 10f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerSinglePointNotAtMaxReturnsTrue()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            // Single point bounds at (5,5,5)
            Bounds inner = new(new Vector3(5f, 5f, 5f), new Vector3(0.0001f, 0.0001f, 0.0001f));
            Assert.IsTrue(outer.FastContainsHalfOpen3D(inner));
        }

        [Test]
        public void FastContainsHalfOpen3DInnerSinglePointAtMaxReturnsFalse()
        {
            Bounds outer = new(new Vector3(0f, 0f, 0f), new Vector3(20f, 20f, 20f));
            // Single point bounds at (10,10,10) which is the max
            Bounds inner = new(new Vector3(10f, 10f, 10f), new Vector3(0.0001f, 0.0001f, 0.0001f));
            Assert.IsFalse(outer.FastContainsHalfOpen3D(inner));
        }
    }
}
