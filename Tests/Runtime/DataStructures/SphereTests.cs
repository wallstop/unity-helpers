namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    [TestFixture]
    public sealed class SphereTests
    {
        // Contains(point) tests
        [Test]
        public void Contains_PointInside_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 2f);
            Assert.IsTrue(s.Contains(new Vector3(1f, 0.5f, 0.5f)));
        }

        [Test]
        public void Contains_PointOnSurface_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 1f);
            Assert.IsTrue(s.Contains(new Vector3(1f, 0f, 0f)));
        }

        [Test]
        public void Contains_PointOutside_ReturnsFalse()
        {
            var s = new Sphere(Vector3.zero, 1f);
            Assert.IsFalse(s.Contains(new Vector3(1.0001f, 0f, 0f)));
        }

        [Test]
        public void Contains_ZeroRadius_OnlyCenterIsTrue()
        {
            var center = new Vector3(3.2f, -5f, 7.7f);
            var s = new Sphere(center, 0f);
            Assert.IsTrue(s.Contains(center));
            Assert.IsFalse(s.Contains(center + Vector3.right * 1e-6f));
        }

        [Test]
        public void Contains_NegativeRadius_BehavesLikePositiveDueToSquaring()
        {
            var center = new Vector3(0.1f, 0.2f, 0.3f);
            var sNeg = new Sphere(center, -2f);
            var sPos = new Sphere(center, 2f);
            Assert.AreEqual(
                sPos.Contains(center + new Vector3(2f, 0f, 0f)),
                sNeg.Contains(center + new Vector3(2f, 0f, 0f))
            );
            Assert.AreEqual(
                sPos.Contains(center + new Vector3(2.0001f, 0f, 0f)),
                sNeg.Contains(center + new Vector3(2.0001f, 0f, 0f))
            );
        }

        // Intersects(Bounds) tests
        [Test]
        public void Intersects_BoundsFarOutside_ReturnsFalse()
        {
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(new Vector3(5f, 5f, 5f), new Vector3(2f, 2f, 2f));
            Assert.IsFalse(s.Intersects(b));
        }

        [Test]
        public void Intersects_BoundsTouchingFace_ReturnsTrue()
        {
            // Sphere at origin radius 1. Bounds begins at x=1, spans across y,z = [-1,1]
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(center: new Vector3(1.5f, 0f, 0f), size: new Vector3(1f, 2f, 2f)); // min.x = 1
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_BoundsTouchingEdge_ReturnsTrue()
        {
            // Use a thin slab that aligns so closest point is exactly (1,1,0) on a radius sqrt(2) sphere
            // For unit sphere, we instead create bounds such that closest point is (1,0,0) along an "edge" (zero thickness in z)
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(center: new Vector3(1.5f, 0f, 0f), size: new Vector3(1f, 2f, 0f)); // an edge along Y at x in [1,2]
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_BoundsTouchingCorner_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 1f);
            // A point-like bounds whose center is on the sphere surface
            var b = new Bounds(center: new Vector3(1f, 0f, 0f), size: Vector3.zero);
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_BoundsFullyInsideSphere_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 5f);
            var b = new Bounds(
                center: new Vector3(0.5f, 0.5f, 0.5f),
                size: new Vector3(1f, 1f, 1f)
            );
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_SphereFullyInsideBounds_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(center: Vector3.zero, size: new Vector3(10f, 10f, 10f));
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_TouchingDueToTolerance_IsTrue()
        {
            // Place bounds so its face is at distance 1 + 1e-7 (just inside tolerance 1e-6)
            var radius = 1f;
            var s = new Sphere(Vector3.zero, radius);
            var epsilon = 1e-7f;
            var b = new Bounds(
                center: new Vector3(1f + epsilon + 0.5f, 0f, 0f),
                size: new Vector3(1f, 2f, 2f)
            ); // min.x = 1 + epsilon
            Assert.IsTrue(s.Intersects(b));
        }

        [Test]
        public void Intersects_OutsideBeyondTolerance_IsFalse()
        {
            var radius = 1f;
            var s = new Sphere(Vector3.zero, radius);
            var epsilon = 2e-6f; // larger than tolerance used in Sphere
            var b = new Bounds(
                center: new Vector3(1f + epsilon + 0.5f, 0f, 0f),
                size: new Vector3(1f, 2f, 2f)
            ); // min.x = 1 + epsilon
            Assert.IsFalse(s.Intersects(b));
        }

        // Overlaps(Bounds) tests: in implementation this means bounds are fully contained in the sphere
        [Test]
        public void Overlaps_BoundsFullyInsideSphere_ReturnsTrue()
        {
            var s = new Sphere(Vector3.zero, 3f);
            var b = new Bounds(
                center: new Vector3(0.5f, 0.5f, 0.5f),
                size: new Vector3(1f, 1f, 1f)
            );
            Assert.IsTrue(s.Overlaps(b));
        }

        [Test]
        public void Overlaps_BoundsPartiallyInside_ReturnsFalse()
        {
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(center: new Vector3(0.8f, 0f, 0f), size: new Vector3(1f, 1f, 1f));
            Assert.IsFalse(s.Overlaps(b));
        }

        [Test]
        public void Overlaps_BoundsCompletelyOutside_ReturnsFalse()
        {
            var s = new Sphere(Vector3.zero, 1f);
            var b = new Bounds(center: new Vector3(5f, 0f, 0f), size: new Vector3(1f, 1f, 1f));
            Assert.IsFalse(s.Overlaps(b));
        }

        [Test]
        public void Overlaps_ZeroSizeBoundsAtCenter_ReturnsTrue()
        {
            var center = new Vector3(2f, -1f, 3f);
            var s = new Sphere(center, 0f);
            var b = new Bounds(center: center, size: Vector3.zero);
            Assert.IsTrue(s.Overlaps(b));
        }
    }
}
