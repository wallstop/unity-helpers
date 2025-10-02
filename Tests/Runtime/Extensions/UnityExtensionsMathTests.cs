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
    }
}
