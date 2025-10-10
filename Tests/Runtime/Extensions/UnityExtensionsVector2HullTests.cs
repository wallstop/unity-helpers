namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class UnityExtensionsVector2HullTests
    {
        [Test]
        public void BuildConvexHullVector2IncludesColinearPointsWhenRequested()
        {
            List<Vector2> points = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 2),
                new Vector2(2, 2),
                new Vector2(2, 0),
                new Vector2(1, 0),
            };

            List<Vector2> hull = points.BuildConvexHull(includeColinearPoints: true);
            CollectionAssert.AreEquivalent(
                points,
                hull,
                "Convex hull should include colinear edge points when requested."
            );
            Assert.AreEqual(
                new Vector2(0, 0),
                hull[0],
                "Hull should start at the lexicographically smallest point."
            );
        }

        [Test]
        public void BuildConvexHullVector2ExcludesColinearPointsWhenDisabled()
        {
            List<Vector2> points = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 2),
                new Vector2(2, 2),
                new Vector2(2, 0),
                new Vector2(1, 0),
            };

            List<Vector2> hull = points.BuildConvexHull(includeColinearPoints: false);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 2),
                    new Vector2(2, 2),
                    new Vector2(2, 0),
                },
                hull,
                "Convex hull should exclude colinear interior points when disabled."
            );
        }

        [Test]
        public void ConvexHullVector2PermutationInvariance()
        {
            List<Vector2> points = new()
            {
                new Vector2(0, 0),
                new Vector2(3, 0),
                new Vector2(3, 3),
                new Vector2(0, 3),
                new Vector2(1, 1),
            };
            List<Vector2> expected = new()
            {
                new Vector2(0, 0),
                new Vector2(3, 0),
                new Vector2(3, 3),
                new Vector2(0, 3),
            };

            List<Vector2> hullA = points.BuildConvexHull(includeColinearPoints: false);
            points.Reverse();
            List<Vector2> hullB = points.BuildConvexHull(includeColinearPoints: false);
            CollectionAssert.AreEquivalent(
                expected,
                hullA,
                "Hull should be invariant under input permutation (A)."
            );
            CollectionAssert.AreEquivalent(
                expected,
                hullB,
                "Hull should be invariant under input permutation (B)."
            );
        }

        [Test]
        public void IsPointInsideConvexHullVector2DetectsContainment()
        {
            List<Vector2> hull = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 4),
                new Vector2(4, 4),
                new Vector2(4, 0),
            };
            Assert.IsTrue(
                hull.IsConvexHullInsideConvexHull(new List<Vector2> { new Vector2(1, 1) }),
                "Point inside square should be reported as inside."
            );
            Assert.IsFalse(
                hull.IsConvexHullInsideConvexHull(new List<Vector2> { new Vector2(5, 5) }),
                "Point outside square should be reported as outside."
            );
        }

        [Test]
        public void IsConvexHullInsideConvexHullVector2ValidatesInteriorPoints()
        {
            List<Vector2> outer = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 4),
                new Vector2(4, 4),
                new Vector2(4, 0),
            };
            List<Vector2> inner = new() { new Vector2(1, 1), new Vector2(2, 2), new Vector2(3, 1) };
            Assert.IsTrue(
                outer.IsConvexHullInsideConvexHull(inner),
                "Inner triangle should be inside outer square."
            );
            inner.Add(new Vector2(-1, 0));
            Assert.IsFalse(
                outer.IsConvexHullInsideConvexHull(inner),
                "Adding a point outside should invalidate inside condition."
            );
        }

        [Test]
        public void ConcaveHullVector2VariantsMatchConvexHullRectangle()
        {
            List<Vector2> points = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 3),
                new Vector2(3, 3),
                new Vector2(3, 0),
            };
            List<Vector2> convex = points.BuildConvexHull(includeColinearPoints: false);
            List<Vector2> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit();
            List<Vector2> concaveKnn = points.BuildConcaveHullKnn();
            List<Vector2> concave = points.BuildConcaveHull(
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            CollectionAssert.AreEquivalent(
                convex,
                concaveEdgeSplit,
                "Edge-split concave hull should reduce to convex hull for rectangle."
            );
            CollectionAssert.AreEquivalent(
                convex,
                concaveKnn,
                "k-NN concave hull should reduce to convex hull for rectangle."
            );
            CollectionAssert.AreEquivalent(
                convex,
                concave,
                "Unified concave hull should reduce to convex hull for rectangle."
            );
        }

        [Test]
        public void ConcaveHullVector2VariantsMatchConvexHullTriangle()
        {
            List<Vector2> points = new()
            {
                new Vector2(0, 0),
                new Vector2(0, 3),
                new Vector2(3, 3),
                new Vector2(3, 0),
            };
            List<Vector2> convex = points.BuildConvexHull(includeColinearPoints: false);
            List<Vector2> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit();
            List<Vector2> concaveKnn = points.BuildConcaveHullKnn();
            List<Vector2> concave = points.BuildConcaveHull(
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            CollectionAssert.AreEquivalent(
                convex,
                concaveEdgeSplit,
                "Edge-split concave hull should reduce to convex hull for rectangle/triangle set."
            );
            CollectionAssert.AreEquivalent(
                convex,
                concaveKnn,
                "k-NN concave hull should reduce to convex hull for rectangle/triangle set."
            );
            CollectionAssert.AreEquivalent(
                convex,
                concave,
                "Unified concave hull should reduce to convex hull for rectangle/triangle set."
            );
        }

        [Test]
        public void ConcaveHullVector2RandomSetsRespectInvariants()
        {
            System.Random rng = new(7);
            for (int t = 0; t < 10; ++t)
            {
                int count = rng.Next(6, 20);
                List<Vector2> points = new(count);
                for (int i = 0; i < count; ++i)
                {
                    points.Add(new Vector2(rng.Next(-10, 11), rng.Next(-10, 11)));
                }

                List<Vector2> convex = points.BuildConvexHull(includeColinearPoints: false);
                List<Vector2> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit();
                List<Vector2> concaveKnn = points.BuildConcaveHullKnn();
                List<Vector2> concave = points.BuildConcaveHull(
                    new UnityExtensions.ConcaveHullOptions
                    {
                        Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                    }
                );

                // No duplicates in results
                Assert.AreEqual(
                    convex.Distinct().Count(),
                    convex.Count,
                    "Convex hull should contain unique vertices."
                );
                Assert.AreEqual(
                    concaveEdgeSplit.Distinct().Count(),
                    concaveEdgeSplit.Count,
                    "Edge-split concave hull should contain unique vertices."
                );
                Assert.AreEqual(
                    concaveKnn.Distinct().Count(),
                    concaveKnn.Count,
                    "k-NN concave hull should contain unique vertices."
                );
                Assert.AreEqual(
                    concave.Distinct().Count(),
                    concave.Count,
                    "Unified concave hull should contain unique vertices."
                );

                // All outputs are subset of inputs
                HashSet<Vector2> inputSet = new(points);
                Assert.IsTrue(
                    convex.All(inputSet.Contains),
                    "All convex hull vertices should be from the input set."
                );
                Assert.IsTrue(
                    concaveEdgeSplit.All(inputSet.Contains),
                    "All edge-split concave hull vertices should be from the input set."
                );
                Assert.IsTrue(
                    concaveKnn.All(inputSet.Contains),
                    "All k-NN concave hull vertices should be from the input set."
                );
                Assert.IsTrue(
                    concave.All(inputSet.Contains),
                    "All unified concave hull vertices should be from the input set."
                );

                // Concave inside convex
                Assert.IsTrue(
                    convex.IsConvexHullInsideConvexHull(concaveEdgeSplit),
                    "Edge-split concave hull must lie inside convex hull."
                );
                Assert.IsTrue(
                    convex.IsConvexHullInsideConvexHull(concaveKnn),
                    "k-NN concave hull must lie inside convex hull."
                );
                Assert.IsTrue(
                    convex.IsConvexHullInsideConvexHull(concave),
                    "Unified concave hull must lie inside convex hull."
                );
            }
        }
    }
}
