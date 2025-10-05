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
        public void ContainsPointInsideReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 2f);
            Assert.IsTrue(s.Contains(new Vector3(1f, 0.5f, 0.5f)));
        }

        [Test]
        public void ContainsPointOnSurfaceReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Assert.IsTrue(s.Contains(new Vector3(1f, 0f, 0f)));
        }

        [Test]
        public void ContainsPointOutsideReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Assert.IsFalse(s.Contains(new Vector3(1.0001f, 0f, 0f)));
        }

        [Test]
        public void ContainsZeroRadiusOnlyCenterIsTrue()
        {
            Vector3 center = new Vector3(3.2f, -5f, 7.7f);
            Sphere s = new Sphere(center, 0f);
            Assert.IsTrue(s.Contains(center));
            Assert.IsFalse(s.Contains(center + Vector3.right * 1e-6f));
        }

        [Test]
        public void ContainsNegativeRadiusBehavesLikePositiveDueToSquaring()
        {
            Vector3 center = new Vector3(0.1f, 0.2f, 0.3f);
            Sphere sNeg = new Sphere(center, -2f);
            Sphere sPos = new Sphere(center, 2f);
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
        public void IntersectsBoundsFarOutsideReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(new Vector3(5f, 5f, 5f), new Vector3(2f, 2f, 2f));
            Assert.IsFalse(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsBoundsTouchingFaceReturnsTrue()
        {
            // Sphere at origin radius 1. Bounds begins at x=1, spans across y,z = [-1,1]
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: new Vector3(1.5f, 0f, 0f), size: new Vector3(1f, 2f, 2f)); // min.x = 1
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsBoundsTouchingEdgeReturnsTrue()
        {
            // Use a thin slab that aligns so closest point is exactly (1,1,0) on a radius sqrt(2) sphere
            // For unit sphere, we instead create bounds such that closest point is (1,0,0) along an "edge" (zero thickness in z)
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: new Vector3(1.5f, 0f, 0f), size: new Vector3(1f, 2f, 0f)); // an edge along Y at x in [1,2]
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsBoundsTouchingCornerReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            // A point-like bounds whose center is on the sphere surface
            Bounds b = new Bounds(center: new Vector3(1f, 0f, 0f), size: Vector3.zero);
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsBoundsFullyInsideSphereReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 5f);
            Bounds b = new Bounds(
                center: new Vector3(0.5f, 0.5f, 0.5f),
                size: new Vector3(1f, 1f, 1f)
            );
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsSphereFullyInsideBoundsReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: Vector3.zero, size: new Vector3(10f, 10f, 10f));
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsTouchingDueToToleranceIsTrue()
        {
            // Place bounds so its face is at distance 1 + 1e-7 (just inside tolerance 1e-6)
            float radius = 1f;
            Sphere s = new Sphere(Vector3.zero, radius);
            float epsilon = 1e-7f;
            Bounds b = new Bounds(
                center: new Vector3(1f + epsilon + 0.5f, 0f, 0f),
                size: new Vector3(1f, 2f, 2f)
            ); // min.x = 1 + epsilon
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsOutsideBeyondToleranceIsFalse()
        {
            float radius = 1f;
            Sphere s = new Sphere(Vector3.zero, radius);
            float epsilon = 2e-6f; // larger than tolerance used in Sphere
            Bounds b = new Bounds(
                center: new Vector3(1f + epsilon + 0.5f, 0f, 0f),
                size: new Vector3(1f, 2f, 2f)
            ); // min.x = 1 + epsilon
            Assert.IsFalse(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        // Overlaps(Bounds) tests: in implementation this means bounds are fully contained in the sphere
        [Test]
        public void OverlapsBoundsFullyInsideSphereReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 3f);
            Bounds b = new Bounds(
                center: new Vector3(0.5f, 0.5f, 0.5f),
                size: new Vector3(1f, 1f, 1f)
            );
            Assert.IsTrue(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsBoundsPartiallyInsideReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: new Vector3(0.8f, 0f, 0f), size: new Vector3(1f, 1f, 1f));
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsBoundsCompletelyOutsideReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: new Vector3(5f, 0f, 0f), size: new Vector3(1f, 1f, 1f));
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsZeroSizeBoundsAtCenterReturnsTrue()
        {
            Vector3 center = new Vector3(2f, -1f, 3f);
            Sphere s = new Sphere(center, 0f);
            Bounds b = new Bounds(center: center, size: Vector3.zero);
            Assert.IsTrue(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsBoundsOnSphereSurfaceReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 5f);
            // Bounds with corner at distance exactly 5 from origin
            Bounds b = new Bounds(center: new Vector3(3f, 3f, 3f), size: new Vector3(2f, 2f, 2f));
            // Max corner is at (4, 4, 4), distance = sqrt(48) ≈ 6.93, outside sphere
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsBoundsWithOnlyOneCornerOutsideReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 2f);
            Bounds b = new Bounds(
                center: new Vector3(0.5f, 0.5f, 1.8f),
                size: new Vector3(1f, 1f, 1f)
            );
            // One corner at (1, 1, 2.3), distance ≈ 2.64, outside radius 2
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsEmptyBoundsReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Assert.IsTrue(s.Overlaps(BoundingBox3D.Empty));
        }

        [Test]
        public void OverlapsBoundsAtOriginWithSphereCenteredElsewhereReturnsExpectedResult()
        {
            Sphere s = new Sphere(new Vector3(10f, 10f, 10f), 2f);
            Bounds b = new Bounds(center: Vector3.zero, size: new Vector3(1f, 1f, 1f));
            // Farthest corner from (10,10,10) is very far away
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsLargeBoundsContainingSphereReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Bounds b = new Bounds(center: Vector3.zero, size: new Vector3(100f, 100f, 100f));
            // Bounds corners are far outside the sphere
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsNegativeRadiusBehavesLikePositiveDueToSquaring()
        {
            Vector3 center = new Vector3(1f, 2f, 3f);
            Sphere sNeg = new Sphere(center, -3f);
            Sphere sPos = new Sphere(center, 3f);
            Bounds b = new Bounds(center: center, size: new Vector3(2f, 2f, 2f));
            Assert.AreEqual(
                sPos.Overlaps(BoundingBox3D.FromClosedBounds(b)),
                sNeg.Overlaps(BoundingBox3D.FromClosedBounds(b))
            );
        }

        [Test]
        public void OverlapsBoundsExactlyFittingInsideSphereReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 5f);
            // Cube centered at origin with corners at (±2, ±2, ±2), farthest distance = sqrt(12) ≈ 3.46
            Bounds b = new Bounds(center: Vector3.zero, size: new Vector3(4f, 4f, 4f));
            Assert.IsTrue(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsBoundsCenteredAwayFromSphereReturnsExpectedResult()
        {
            Sphere s = new Sphere(new Vector3(5f, 0f, 0f), 3f);
            Bounds b = new Bounds(center: new Vector3(7f, 0f, 0f), size: new Vector3(2f, 2f, 2f));
            // Closest corner to sphere center: (6, -1, -1), distance ≈ sqrt(1 + 1 + 1) ≈ 1.73
            // Farthest corner: (8, 1, 1), distance ≈ sqrt(9 + 1 + 1) ≈ 3.32, outside radius 3
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsAsymmetricBoundsReturnsExpectedResult()
        {
            Sphere s = new Sphere(Vector3.zero, 10f);
            Bounds b = new Bounds(center: new Vector3(2f, 3f, 4f), size: new Vector3(6f, 2f, 4f));
            // Max corner at (5, 4, 6), distance = sqrt(25 + 16 + 36) = sqrt(77) ≈ 8.77
            Assert.IsTrue(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void OverlapsZeroRadiusSphereWithNonZeroBoundsReturnsFalse()
        {
            Vector3 center = new Vector3(1f, 2f, 3f);
            Sphere s = new Sphere(center, 0f);
            Bounds b = new Bounds(center: center, size: new Vector3(1f, 1f, 1f));
            // Even small bounds won't fit in zero-radius sphere
            Assert.IsFalse(s.Overlaps(BoundingBox3D.FromClosedBounds(b)));
        }

        // Contains(point) edge cases
        [Test]
        public void ContainsVeryClosePointDueToFloatingPointReturnsExpectedResult()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Vector3 almostOnSurface = new Vector3(1f - 1e-7f, 0f, 0f);
            Assert.IsTrue(s.Contains(almostOnSurface));
        }

        [Test]
        public void ContainsPointAtMaxFloatDistanceReturnsFalse()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Assert.IsFalse(s.Contains(new Vector3(float.MaxValue, 0f, 0f)));
        }

        // Intersects edge cases
        [Test]
        public void IntersectsVerySmallBoundsInsideSphereReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 5f);
            Bounds b = new Bounds(
                center: new Vector3(1f, 1f, 1f),
                size: new Vector3(0.01f, 0.01f, 0.01f)
            );
            Assert.IsTrue(s.Intersects(BoundingBox3D.FromClosedBounds(b)));
        }

        [Test]
        public void IntersectsEmptyBoundsReturnsTrue()
        {
            Sphere s = new Sphere(Vector3.zero, 1f);
            Assert.IsTrue(s.Intersects(BoundingBox3D.Empty));
        }

        [Test]
        public void IntersectsNegativeRadiusBehavesLikePositive()
        {
            Vector3 center = new Vector3(2f, 3f, 4f);
            Sphere sNeg = new Sphere(center, -2f);
            Sphere sPos = new Sphere(center, 2f);
            Bounds b = new Bounds(center: new Vector3(3f, 3f, 4f), size: new Vector3(1f, 1f, 1f));
            Assert.AreEqual(
                sPos.Intersects(BoundingBox3D.FromClosedBounds(b)),
                sNeg.Intersects(BoundingBox3D.FromClosedBounds(b))
            );
        }
    }
}
