namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    [TestFixture]
    public sealed class BoundingBox3DTests
    {
        [Test]
        public void ConstructorWithValidBoundsCreatesBox()
        {
            Vector3 min = new(0f, 0f, 0f);
            Vector3 max = new(10f, 10f, 10f);
            BoundingBox3D box = new(min, max);

            Assert.AreEqual(min, box.min);
        }

        [Test]
        public void ConstructorWithEqualMinMaxCreatesValidBox()
        {
            Vector3 point = new(5f, 5f, 5f);
            BoundingBox3D box = new(point, point);

            Assert.AreEqual(point, box.min);
            Assert.IsFalse(box.IsEmpty);
        }

        [Test]
        public void ConstructorWithInvalidBoundsThrowsException()
        {
            Vector3 min = new(10f, 10f, 10f);
            Vector3 max = new(0f, 0f, 0f);

            Assert.Throws<System.ArgumentException>(() => new BoundingBox3D(min, max));
        }

        [Test]
        public void ConstructorWithPartiallyInvalidBoundsThrowsException()
        {
            Vector3 min = new(0f, 10f, 0f);
            Vector3 max = new(10f, 0f, 10f);

            Assert.Throws<System.ArgumentException>(() => new BoundingBox3D(min, max));
        }

        [Test]
        public void CenterReturnsCorrectMidpoint()
        {
            Vector3 min = new(0f, 0f, 0f);
            Vector3 max = new(10f, 10f, 10f);
            BoundingBox3D box = new(min, max);

            Vector3 expectedCenter = new(5f, 5f, 5f);
            Assert.AreEqual(expectedCenter.x, box.Center.x, 0.0001f);
            Assert.AreEqual(expectedCenter.y, box.Center.y, 0.0001f);
            Assert.AreEqual(expectedCenter.z, box.Center.z, 0.0001f);
        }

        [Test]
        public void SizeReturnsCorrectDimensions()
        {
            Vector3 min = new(-5f, -5f, -5f);
            Vector3 max = new(5f, 5f, 5f);
            BoundingBox3D box = new(min, max);

            Vector3 expectedSize = new(10f, 10f, 10f);
            Assert.AreEqual(expectedSize.x, box.Size.x, 0.0001f);
            Assert.AreEqual(expectedSize.y, box.Size.y, 0.0001f);
            Assert.AreEqual(expectedSize.z, box.Size.z, 0.0001f);
        }

        [Test]
        public void IsEmptyReturnsTrueForDefaultBox()
        {
            BoundingBox3D box = default;
            Assert.IsTrue(box.IsEmpty);
        }

        [Test]
        public void IsEmptyReturnsFalseForValidBox()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Assert.IsFalse(box.IsEmpty);
        }

        [Test]
        public void EmptyPropertyReturnsEmptyBox()
        {
            BoundingBox3D empty = BoundingBox3D.Empty;
            Assert.IsTrue(empty.IsEmpty);
        }

        [Test]
        public void FromCenterAndSizeCreatesCorrectBox()
        {
            Vector3 center = new(5f, 5f, 5f);
            Vector3 size = new(10f, 10f, 10f);
            BoundingBox3D box = BoundingBox3D.FromCenterAndSize(center, size);

            Assert.AreEqual(new Vector3(0f, 0f, 0f), box.min);
            Assert.AreEqual(center.x, box.Center.x, 0.0001f);
            Assert.AreEqual(center.y, box.Center.y, 0.0001f);
            Assert.AreEqual(center.z, box.Center.z, 0.0001f);
        }

        [Test]
        public void FromCenterAndSizeWithZeroSizeCreatesPoint()
        {
            Vector3 center = new(5f, 5f, 5f);
            BoundingBox3D box = BoundingBox3D.FromCenterAndSize(center, Vector3.zero);

            Assert.IsFalse(box.IsEmpty);
            Assert.AreEqual(center, box.min);
        }

        [Test]
        public void FromClosedBoundsCreatesCorrectBox()
        {
            Bounds bounds = new(new Vector3(5f, 5f, 5f), new Vector3(10f, 10f, 10f));
            BoundingBox3D box = BoundingBox3D.FromClosedBounds(bounds);

            Assert.AreEqual(bounds.min, box.min);
        }

        [Test]
        public void FromPointCreatesValidBox()
        {
            Vector3 point = new(3f, 4f, 5f);
            BoundingBox3D box = BoundingBox3D.FromPoint(point);

            Assert.AreEqual(point, box.min);
            Assert.IsFalse(box.IsEmpty);
        }

        [Test]
        public void ContainsPointInsideReturnsTrue()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 5f);

            Assert.IsTrue(box.Contains(point));
        }

        [Test]
        public void ContainsPointOnMinBoundaryReturnsTrue()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Assert.IsTrue(box.Contains(Vector3.zero));
        }

        [Test]
        public void ContainsPointOnMaxBoundaryReturnsFalse()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Assert.IsFalse(box.Contains(Vector3.one));
        }

        [Test]
        public void ContainsPointOutsideReturnsFalse()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Vector3 point = new(2f, 2f, 2f);

            Assert.IsFalse(box.Contains(point));
        }

        [Test]
        public void ContainsPointJustBelowMinReturnsFalse()
        {
            BoundingBox3D box = new(Vector3.one, new Vector3(10f, 10f, 10f));
            Vector3 point = new(0.999f, 5f, 5f);

            Assert.IsFalse(box.Contains(point));
        }

        [Test]
        public void ContainsBoxCompletelyInsideReturnsTrue()
        {
            BoundingBox3D outer = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D inner = new(new Vector3(2f, 2f, 2f), new Vector3(8f, 8f, 8f));

            Assert.IsTrue(outer.Contains(inner));
        }

        [Test]
        public void ContainsBoxPartiallyOutsideReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D box2 = new(new Vector3(5f, 5f, 5f), new Vector3(15f, 15f, 15f));

            Assert.IsFalse(box1.Contains(box2));
        }

        [Test]
        public void ContainsBoxCompletelyOutsideReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(new Vector3(10f, 10f, 10f), new Vector3(20f, 20f, 20f));

            Assert.IsFalse(box1.Contains(box2));
        }

        [Test]
        public void ContainsIdenticalBoxReturnsTrue()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(Vector3.zero, Vector3.one);

            Assert.IsTrue(box1.Contains(box2));
        }

        [Test]
        public void ContainsEmptyBoxReturnsTrue()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Assert.IsTrue(box.Contains(BoundingBox3D.Empty));
        }

        [Test]
        public void IntersectsOverlappingBoxesReturnsTrue()
        {
            BoundingBox3D box1 = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D box2 = new(new Vector3(5f, 5f, 5f), new Vector3(15f, 15f, 15f));

            Assert.IsTrue(box1.Intersects(box2));
            Assert.IsTrue(box2.Intersects(box1));
        }

        [Test]
        public void IntersectsTouchingBoxesReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(Vector3.one, new Vector3(2f, 2f, 2f));

            Assert.IsFalse(box1.Intersects(box2));
        }

        [Test]
        public void IntersectsNonOverlappingBoxesReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(new Vector3(2f, 2f, 2f), new Vector3(3f, 3f, 3f));

            Assert.IsFalse(box1.Intersects(box2));
        }

        [Test]
        public void IntersectsBoxInsideAnotherReturnsTrue()
        {
            BoundingBox3D outer = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D inner = new(new Vector3(2f, 2f, 2f), new Vector3(8f, 8f, 8f));

            Assert.IsTrue(outer.Intersects(inner));
            Assert.IsTrue(inner.Intersects(outer));
        }

        [Test]
        public void IntersectsOnSingleAxisReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(new Vector3(0.5f, 2f, 0.5f), new Vector3(1.5f, 3f, 1.5f));

            Assert.IsFalse(box1.Intersects(box2));
        }

        [Test]
        public void IntersectsEmptyBoxReturnsFalse()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Assert.IsFalse(box.Intersects(BoundingBox3D.Empty));
        }

        [Test]
        public void ExpandToIncludePointInsideReturnsUnchanged()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 5f);
            BoundingBox3D expanded = box.ExpandToInclude(point);

            Assert.AreEqual(box.min, expanded.min);
        }

        [Test]
        public void ExpandToIncludePointOutsideExpandsBox()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Vector3 point = new(5f, 5f, 5f);
            BoundingBox3D expanded = box.ExpandToInclude(point);

            Assert.IsTrue(expanded.Contains(point));
            Assert.AreEqual(Vector3.zero, expanded.min);
        }

        [Test]
        public void ExpandToIncludePointBelowMinExpandsMin()
        {
            BoundingBox3D box = new(Vector3.one, new Vector3(10f, 10f, 10f));
            Vector3 point = Vector3.zero;
            BoundingBox3D expanded = box.ExpandToInclude(point);

            Assert.AreEqual(Vector3.zero, expanded.min);
        }

        [Test]
        public void ExpandToIncludePointAboveMaxExpandsMax()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Vector3 point = new(5f, 5f, 5f);
            BoundingBox3D expanded = box.ExpandToInclude(point);

            Assert.IsTrue(expanded.Contains(point));
        }

        [Test]
        public void ExpandToIncludeBoxCompletelyInsideReturnsUnchanged()
        {
            BoundingBox3D outer = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D inner = new(new Vector3(2f, 2f, 2f), new Vector3(8f, 8f, 8f));
            BoundingBox3D expanded = outer.ExpandToInclude(inner);

            Assert.AreEqual(outer.min, expanded.min);
        }

        [Test]
        public void ExpandToIncludeBoxPartiallyOutsideExpands()
        {
            BoundingBox3D box1 = new(Vector3.zero, new Vector3(5f, 5f, 5f));
            BoundingBox3D box2 = new(new Vector3(3f, 3f, 3f), new Vector3(10f, 10f, 10f));
            BoundingBox3D expanded = box1.ExpandToInclude(box2);

            Assert.AreEqual(Vector3.zero, expanded.min);
            Assert.IsTrue(expanded.Contains(box2));
        }

        [Test]
        public void ExpandToIncludeEmptyBoxReturnsUnchanged()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            BoundingBox3D expanded = box.ExpandToInclude(BoundingBox3D.Empty);

            Assert.AreEqual(box.min, expanded.min);
        }

        [Test]
        public void EmptyBoxExpandToIncludeBoxReturnsOtherBox()
        {
            BoundingBox3D empty = BoundingBox3D.Empty;
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            BoundingBox3D result = empty.ExpandToInclude(box);

            Assert.AreEqual(box.min, result.min);
        }

        [Test]
        public void EnsureMinimumSizeWithSmallerBoxExpands()
        {
            BoundingBox3D box = new(new Vector3(5f, 5f, 5f), new Vector3(6f, 6f, 6f));
            BoundingBox3D expanded = box.EnsureMinimumSize(5f);

            Vector3 size = expanded.Size;
            Assert.GreaterOrEqual(size.x, 5f);
            Assert.GreaterOrEqual(size.y, 5f);
            Assert.GreaterOrEqual(size.z, 5f);
        }

        [Test]
        public void EnsureMinimumSizeWithLargerBoxReturnsUnchanged()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            BoundingBox3D result = box.EnsureMinimumSize(5f);

            Assert.AreEqual(box.min, result.min);
        }

        [Test]
        public void EnsureMinimumSizeWithZeroMinimumReturnsUnchanged()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            BoundingBox3D result = box.EnsureMinimumSize(0f);

            Assert.AreEqual(box.min, result.min);
        }

        [Test]
        public void EnsureMinimumSizeWithNegativeMinimumReturnsUnchanged()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            BoundingBox3D result = box.EnsureMinimumSize(-5f);

            Assert.AreEqual(box.min, result.min);
        }

        [Test]
        public void EnsureMinimumSizeOnEmptyBoxReturnsEmpty()
        {
            BoundingBox3D empty = BoundingBox3D.Empty;
            BoundingBox3D result = empty.EnsureMinimumSize(5f);

            Assert.IsTrue(result.IsEmpty);
        }

        [Test]
        public void EnsureMinimumSizeExpandsSymmetrically()
        {
            Vector3 center = new(5f, 5f, 5f);
            BoundingBox3D box = BoundingBox3D.FromCenterAndSize(center, Vector3.one);
            BoundingBox3D expanded = box.EnsureMinimumSize(5f);

            Vector3 expandedCenter = expanded.Center;
            Assert.AreEqual(center.x, expandedCenter.x, 0.01f);
            Assert.AreEqual(center.y, expandedCenter.y, 0.01f);
            Assert.AreEqual(center.z, expandedCenter.z, 0.01f);
        }

        [Test]
        public void ClosestPointInsideReturnsPoint()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 5f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.AreEqual(point, closest);
        }

        [Test]
        public void ClosestPointOutsideReturnsBoundaryPoint()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(15f, 15f, 15f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.IsTrue(
                box.Contains(closest)
                    || Mathf.Approximately(closest.x, box.max.x)
                    || Mathf.Approximately(closest.y, box.max.y)
                    || Mathf.Approximately(closest.z, box.max.z)
            );
        }

        [Test]
        public void ClosestPointOnFaceReturnsFacePoint()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(15f, 5f, 5f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.AreEqual(5f, closest.y);
            Assert.AreEqual(5f, closest.z);
        }

        [Test]
        public void ClosestPointOnEdgeReturnsEdgePoint()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(15f, 15f, 5f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.AreEqual(5f, closest.z);
        }

        [Test]
        public void ClosestPointOnCornerReturnsCorner()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(-5f, -5f, -5f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.AreEqual(Vector3.zero, closest);
        }

        [Test]
        public void ClosestPointOnMaxBoundaryReturnsBoundary()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(20f, 5f, 5f);
            Vector3 closest = box.ClosestPoint(point);

            Assert.AreEqual(box.max.x, closest.x);
        }

        [Test]
        public void DistanceSquaredToPointInsideReturnsZero()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(5f, 5f, 5f);
            float distSq = box.DistanceSquaredTo(point);

            Assert.AreEqual(0f, distSq, 0.0001f);
        }

        [Test]
        public void DistanceSquaredToPointOutsideReturnsCorrectDistance()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = new(13f, 10f, 10f);
            float distSq = box.DistanceSquaredTo(point);

            Assert.AreEqual(9f, distSq, 0.0001f);
        }

        [Test]
        public void DistanceSquaredToPointOnBoundaryReturnsZero()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 point = Vector3.zero;
            float distSq = box.DistanceSquaredTo(point);

            Assert.AreEqual(0f, distSq, 0.0001f);
        }

        [Test]
        public void ToBoundsCreatesCorrectBounds()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Bounds bounds = box.ToBounds();

            Assert.AreEqual(box.Size.x, bounds.size.x, 0.0001f);
            Assert.AreEqual(box.Size.y, bounds.size.y, 0.0001f);
            Assert.AreEqual(box.Size.z, bounds.size.z, 0.0001f);
        }

        [Test]
        public void ToBoundsPreservesCenter()
        {
            Vector3 center = new(5f, 5f, 5f);
            BoundingBox3D box = BoundingBox3D.FromCenterAndSize(center, new Vector3(10f, 10f, 10f));
            Bounds bounds = box.ToBounds();

            Assert.AreEqual(center.x, bounds.center.x, 0.01f);
            Assert.AreEqual(center.y, bounds.center.y, 0.01f);
            Assert.AreEqual(center.z, bounds.center.z, 0.01f);
        }

        [Test]
        public void HalfOpenSemanticsMaxBoundaryExcluded()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);

            Assert.IsTrue(box.Contains(Vector3.zero));
            Assert.IsFalse(box.Contains(Vector3.one));
            Assert.IsTrue(box.Contains(new Vector3(0.999f, 0.999f, 0.999f)));
        }

        [Test]
        public void NegativeCoordinatesWork()
        {
            BoundingBox3D box = new(new Vector3(-10f, -10f, -10f), Vector3.zero);

            Assert.IsTrue(box.Contains(new Vector3(-5f, -5f, -5f)));
            Assert.IsFalse(box.Contains(new Vector3(1f, 1f, 1f)));
        }

        [Test]
        public void VerySmallBoxWorks()
        {
            float epsilon = 0.0001f;
            BoundingBox3D box = new(Vector3.zero, new Vector3(epsilon, epsilon, epsilon));

            Assert.IsFalse(box.IsEmpty);
            Assert.IsTrue(box.Contains(new Vector3(epsilon / 2f, epsilon / 2f, epsilon / 2f)));
        }

        [Test]
        public void VeryLargeBoxWorks()
        {
            float large = 1000000f;
            BoundingBox3D box = new(
                new Vector3(-large, -large, -large),
                new Vector3(large, large, large)
            );

            Assert.IsTrue(box.Contains(Vector3.zero));
            Assert.IsTrue(box.Contains(new Vector3(large / 2f, large / 2f, large / 2f)));
        }

        [Test]
        public void AsymmetricBoxWorks()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(1f, 10f, 100f));

            Assert.AreEqual(1f, box.Size.x, 0.0001f);
            Assert.AreEqual(10f, box.Size.y, 0.0001f);
            Assert.AreEqual(100f, box.Size.z, 0.0001f);
        }

        [Test]
        public void ExpandToIncludeMultiplePointsWorks()
        {
            BoundingBox3D box = BoundingBox3D.FromPoint(Vector3.zero);
            box = box.ExpandToInclude(new Vector3(10f, 0f, 0f));
            box = box.ExpandToInclude(new Vector3(0f, 10f, 0f));
            box = box.ExpandToInclude(new Vector3(0f, 0f, 10f));

            Assert.IsTrue(box.Contains(Vector3.zero));
            Assert.IsTrue(box.Contains(new Vector3(5f, 5f, 5f)));
        }

        [Test]
        public void IntersectsEdgeCaseTouchingAtSinglePointReturnsFalse()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(Vector3.one, new Vector3(2f, 2f, 2f));

            Assert.IsFalse(box1.Intersects(box2));
        }

        [Test]
        public void ContainsBoxEdgeCaseExactFit()
        {
            BoundingBox3D box1 = new(Vector3.zero, Vector3.one);
            BoundingBox3D box2 = new(Vector3.zero, Vector3.one);

            Assert.IsTrue(box1.Contains(box2));
        }

        [Test]
        public void FromClosedBoundsHandlesPointBounds()
        {
            Bounds bounds = new(Vector3.zero, Vector3.zero);
            BoundingBox3D box = BoundingBox3D.FromClosedBounds(bounds);

            Assert.IsFalse(box.IsEmpty);
        }

        [Test]
        public void ClosestPointHandlesCenterOfBox()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));
            Vector3 center = box.Center;
            Vector3 closest = box.ClosestPoint(center);

            Assert.AreEqual(center, closest);
        }

        [Test]
        public void MultipleExpandOperationsPreserveCorrectness()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Vector3 point1 = new(5f, 5f, 5f);
            Vector3 point2 = new(-5f, -5f, -5f);

            box = box.ExpandToInclude(point1);
            box = box.ExpandToInclude(point2);

            Assert.IsTrue(box.Contains(point1));
            Assert.IsTrue(box.Contains(point2));
            Assert.IsTrue(box.Contains(Vector3.zero));
        }

        [Test]
        public void DistanceSquaredCalculationCorrect()
        {
            BoundingBox3D box = new(Vector3.zero, Vector3.one);
            Vector3 point = new(2f, 2f, 2f);
            float distSq = box.DistanceSquaredTo(point);

            float expected = 3f;
            Assert.AreEqual(expected, distSq, 0.01f);
        }

        [Test]
        public void EnsureMinimumSizeOnOneAxisOnly()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(0.5f, 10f, 10f));
            BoundingBox3D expanded = box.EnsureMinimumSize(5f);

            Assert.GreaterOrEqual(expanded.Size.x, 5f);
            Assert.GreaterOrEqual(expanded.Size.y, 5f);
            Assert.GreaterOrEqual(expanded.Size.z, 5f);
        }

        [Test]
        public void ContainsHandlesMinMaxBoundaryCorrectly()
        {
            BoundingBox3D box = new(Vector3.zero, new Vector3(10f, 10f, 10f));

            Assert.IsTrue(box.Contains(box.min));
            Assert.IsFalse(box.Contains(box.max));

            Vector3 justBelowMax = box.max - new Vector3(0.0001f, 0.0001f, 0.0001f);
            Assert.IsTrue(box.Contains(justBelowMax));
        }

        [Test]
        public void IntersectsIsSymmetric()
        {
            BoundingBox3D box1 = new(Vector3.zero, new Vector3(5f, 5f, 5f));
            BoundingBox3D box2 = new(new Vector3(3f, 3f, 3f), new Vector3(8f, 8f, 8f));

            Assert.AreEqual(box1.Intersects(box2), box2.Intersects(box1));
        }

        [Test]
        public void FromPointAndExpandMaintainsPointInclusion()
        {
            Vector3 point = new(3.7f, 2.1f, 9.5f);
            BoundingBox3D box = BoundingBox3D.FromPoint(point);

            Assert.IsTrue(box.Contains(point));
        }

        [Test]
        public void ToBoundsRoundTripPreservesApproximateProperties()
        {
            BoundingBox3D original = new(new Vector3(-5f, -5f, -5f), new Vector3(5f, 5f, 5f));
            Bounds bounds = original.ToBounds();
            BoundingBox3D reconstructed = BoundingBox3D.FromClosedBounds(bounds);

            Assert.AreEqual(original.min.x, reconstructed.min.x, 0.01f);
            Assert.AreEqual(original.min.y, reconstructed.min.y, 0.01f);
            Assert.AreEqual(original.min.z, reconstructed.min.z, 0.01f);
        }
    }
}
