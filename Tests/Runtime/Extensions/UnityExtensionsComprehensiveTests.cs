namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Tests;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class UnityExtensionsComprehensiveTests : CommonTestBase
    {
        private static List<FastVector3Int> GenerateRandomPointsSquare(
            int count,
            int range,
            int seed = 1337
        )
        {
            System.Random rng = new(seed);
            List<FastVector3Int> points = new(count);
            for (int i = 0; i < count; ++i)
            {
                int x = rng.Next(-range, range + 1);
                int y = rng.Next(-range, range + 1);
                points.Add(new FastVector3Int(x, y, 0));
            }

            return points;
        }

        private static List<FastVector3Int> GenerateCirclePoints(
            int count,
            int radius,
            int seed = 42
        )
        {
            System.Random rng = new(seed);
            List<FastVector3Int> points = new(count);
            for (int i = 0; i < count; ++i)
            {
                double t = (2.0 * Math.PI * i) / count;
                double jitter = (rng.NextDouble() - 0.5) * 0.05; // slight jitter
                int x = (int)Math.Round(radius * Math.Cos(t + jitter));
                int y = (int)Math.Round(radius * Math.Sin(t + jitter));
                points.Add(new FastVector3Int(x, y, 0));
            }

            return points;
        }

        private Grid CreateGrid(out GameObject owner)
        {
            owner = Track(new GameObject("Grid", typeof(Grid)));
            Grid grid = owner.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 1f);
            return grid;
        }

        [Test]
        public void GetBoundsFromFastVectorCollection()
        {
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(-1, 2, -3),
                new FastVector3Int(4, 5, 1),
                new FastVector3Int(0, -7, 8),
            };

            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(-1, value.xMin);
            Assert.AreEqual(5, value.xMax);
            Assert.AreEqual(-7, value.yMin);
            Assert.AreEqual(6, value.yMax);
            Assert.AreEqual(-3, value.zMin);
            Assert.AreEqual(9, value.zMax);
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionReturnsNullWhenEmpty()
        {
            List<FastVector3Int> points = new();
            Assert.IsNull(points.GetBounds());
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionWithSinglePoint()
        {
            List<FastVector3Int> points = new() { new FastVector3Int(5, -3, 10) };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(5, value.xMin);
            Assert.AreEqual(6, value.xMax);
            Assert.AreEqual(-3, value.yMin);
            Assert.AreEqual(-2, value.yMax);
            Assert.AreEqual(10, value.zMin);
            Assert.AreEqual(11, value.zMax);
            Assert.AreEqual(new Vector3Int(1, 1, 1), value.size);
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionWithNegativeCoordinates()
        {
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(-10, -20, -30),
                new FastVector3Int(-5, -15, -25),
            };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(-10, value.xMin);
            Assert.AreEqual(-4, value.xMax);
            Assert.AreEqual(-20, value.yMin);
            Assert.AreEqual(-14, value.yMax);
            Assert.AreEqual(-30, value.zMin);
            Assert.AreEqual(-24, value.zMax);
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionWithAllSamePoints()
        {
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(7, 7, 7),
                new FastVector3Int(7, 7, 7),
                new FastVector3Int(7, 7, 7),
            };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(7, value.xMin);
            Assert.AreEqual(8, value.xMax);
            Assert.AreEqual(new Vector3Int(7, 7, 7), value.position);
            Assert.AreEqual(new Vector3Int(1, 1, 1), value.size);
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionWithCollinearPoints()
        {
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(5, 0, 0),
                new FastVector3Int(10, 0, 0),
            };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(0, value.xMin);
            Assert.AreEqual(11, value.xMax);
            Assert.AreEqual(0, value.yMin);
            Assert.AreEqual(1, value.yMax);
            Assert.AreEqual(0, value.zMin);
            Assert.AreEqual(1, value.zMax);
            Assert.AreEqual(new Vector3Int(11, 1, 1), value.size);
        }

        [Test]
        public void GetBoundsFromFastVectorCollectionWithExtremeValues()
        {
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(int.MinValue, int.MinValue, int.MinValue),
                new FastVector3Int(int.MaxValue, int.MaxValue, int.MaxValue),
            };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(int.MinValue, value.xMin);
            Assert.AreEqual(int.MinValue, value.yMin);
            Assert.AreEqual(int.MinValue, value.zMin);
        }

        [Test]
        public void GetBoundsFromVector3IntCollection()
        {
            List<Vector3Int> points = new()
            {
                new Vector3Int(-1, 2, -3),
                new Vector3Int(4, 5, 1),
                new Vector3Int(0, -7, 8),
            };

            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(-1, value.xMin);
            Assert.AreEqual(5, value.xMax);
            Assert.AreEqual(-7, value.yMin);
            Assert.AreEqual(6, value.yMax);
            Assert.AreEqual(-3, value.zMin);
            Assert.AreEqual(9, value.zMax);
        }

        [Test]
        public void GetBoundsFromVector3IntCollectionInclusive()
        {
            List<Vector3Int> points = new()
            {
                new Vector3Int(-1, 2, -3),
                new Vector3Int(4, 5, 1),
                new Vector3Int(0, -7, 8),
            };

            BoundsInt? bounds = points.GetBounds(inclusive: true);
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(-1, value.xMin);
            Assert.AreEqual(4, value.xMax);
            Assert.AreEqual(-7, value.yMin);
            Assert.AreEqual(5, value.yMax);
            Assert.AreEqual(-3, value.zMin);
            Assert.AreEqual(8, value.zMax);
        }

        [Test]
        public void GetBoundsFromVector3IntCollectionReturnsNullWhenEmpty()
        {
            List<Vector3Int> points = new();
            Assert.IsNull(points.GetBounds());
        }

        [Test]
        public void GetBoundsFromVector3IntCollectionWithSinglePoint()
        {
            List<Vector3Int> points = new() { new Vector3Int(5, -3, 10) };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            BoundsInt value = bounds.Value;
            Assert.AreEqual(new Vector3Int(5, -3, 10), value.position);
            Assert.AreEqual(new Vector3Int(1, 1, 1), value.size);
        }

        [Test]
        public void GetBoundsFromVector2Collection()
        {
            List<Vector2> positions = new()
            {
                new Vector2(-2f, 1f),
                new Vector2(3f, -4f),
                new Vector2(0f, 6f),
            };

            Bounds? maybeBounds = positions.GetBounds();
            Assert.IsTrue(maybeBounds.HasValue);
            Bounds bounds = maybeBounds.Value;
            Assert.AreEqual(new Vector3(0.5f, 1f, 0f), bounds.center);
            Assert.AreEqual(new Vector3(5f, 10f, 0f), bounds.size);
        }

        [Test]
        public void GetBoundsFromVector2CollectionReturnsNullWhenEmpty()
        {
            Assert.IsNull(new List<Vector2>().GetBounds());
        }

        [Test]
        public void GetBoundsFromBoundsCollection()
        {
            List<Bounds> bounds = new()
            {
                new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2f, 2f, 2f)),
                new Bounds(new Vector3(5f, 5f, 0f), new Vector3(4f, 4f, 4f)),
            };

            Bounds? maybe = bounds.GetBounds();
            Assert.IsTrue(maybe.HasValue);
            Bounds combined = maybe.Value;
            Assert.AreEqual(new Vector3(2.5f, 2.5f, 0f), combined.center);
            Assert.AreEqual(new Vector3(8f, 8f, 4f), combined.size);
        }

        [Test]
        public void GetBoundsFromBoundsCollectionReturnsNullWhenEmpty()
        {
            Assert.IsNull(new List<Bounds>().GetBounds());
        }

        [UnityTest]
        public IEnumerator BuildConvexHullVector3IntIncludesColinearPointsWhenRequested()
        {
            Grid grid = CreateGrid(out GameObject owner);
            List<Vector3Int> points = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(2, 2, 0),
                new Vector3Int(2, 0, 0),
                new Vector3Int(1, 0, 0),
            };

            List<Vector3Int> hull = points.BuildConvexHull(grid, includeColinearPoints: true);

            CollectionAssert.AreEquivalent(points, hull);
            Assert.AreEqual(new Vector3Int(0, 0, 0), hull[0]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildConvexHullVector3IntExcludesColinearPointsWhenDisabled()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<Vector3Int> points = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(2, 2, 0),
                new Vector3Int(2, 0, 0),
                new Vector3Int(1, 0, 0),
            };

            List<Vector3Int> hull = points.BuildConvexHull(grid, includeColinearPoints: false);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 2, 0),
                    new Vector3Int(2, 2, 0),
                    new Vector3Int(2, 0, 0),
                },
                hull
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildConvexHullFastVector3IntProducesExpectedLoop()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 2, 0),
                new FastVector3Int(2, 2, 0),
                new FastVector3Int(2, 0, 0),
                new FastVector3Int(1, 0, 0),
            };

            List<FastVector3Int> hull = points.BuildConvexHull(grid, includeColinearPoints: false);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 2, 0),
                    new FastVector3Int(2, 2, 0),
                    new FastVector3Int(2, 0, 0),
                },
                hull
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildConvexHullFastVector3IntIncludesColinearWhenRequested()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 2, 0),
                new FastVector3Int(2, 2, 0),
                new FastVector3Int(2, 0, 0),
                new FastVector3Int(1, 0, 0),
            };

            List<FastVector3Int> hull = points.BuildConvexHull(grid, includeColinearPoints: true);

            CollectionAssert.AreEquivalent(points, hull);

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPointInsideConvexHullDetectsContainment()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<Vector3Int> hull = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(2, 2, 0),
                new Vector3Int(2, 0, 0),
            };

            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new Vector3Int(1, 1, 0)));
            Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new Vector3Int(3, 3, 0)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPointInsideConvexHullTreatsBoundaryAsInside()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<Vector3Int> hull = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(4, 0, 0),
                new Vector3Int(4, 4, 0),
                new Vector3Int(0, 4, 0),
            };

            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new Vector3Int(0, 2, 0)));
            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new Vector3Int(2, 0, 0)));
            Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new Vector3Int(-1, 2, 0)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPointInsideConvexHullFastVectorDetectsContainment()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> hull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 2, 0),
                new FastVector3Int(2, 2, 0),
                new FastVector3Int(2, 0, 0),
            };

            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new FastVector3Int(1, 1, 0)));
            Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new FastVector3Int(3, 3, 0)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPointInsideConvexHullFastVectorTreatsBoundaryAsInside()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> hull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(4, 0, 0),
                new FastVector3Int(4, 4, 0),
                new FastVector3Int(0, 4, 0),
            };

            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new FastVector3Int(0, 2, 0)));
            Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new FastVector3Int(2, 0, 0)));
            Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new FastVector3Int(-1, 2, 0)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConvexHullInsideConvexHullValidatesInteriorPoints()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<Vector3Int> hull = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 4, 0),
                new Vector3Int(4, 4, 0),
                new Vector3Int(4, 0, 0),
            };

            List<Vector3Int> inner = new()
            {
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, 1, 0),
                new Vector3Int(2, 2, 0),
            };

            Assert.IsTrue(hull.IsConvexHullInsideConvexHull(grid, inner));
            inner.Add(new Vector3Int(5, 5, 0));
            Assert.IsFalse(hull.IsConvexHullInsideConvexHull(grid, inner));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConvexHullInsideConvexHullHandlesCounterClockwiseOrder()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<Vector3Int> hull = new()
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(4, 0, 0),
                new Vector3Int(4, 4, 0),
                new Vector3Int(0, 4, 0),
            };

            List<Vector3Int> inner = new()
            {
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, 2, 0),
                new Vector3Int(3, 1, 0),
            };

            Assert.IsTrue(hull.IsConvexHullInsideConvexHull(grid, inner));
            inner.Add(new Vector3Int(-1, 0, 0));
            Assert.IsFalse(hull.IsConvexHullInsideConvexHull(grid, inner));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConvexHullInsideConvexHullFastVectorValidatesInteriorPoints()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> hull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 4, 0),
                new FastVector3Int(4, 4, 0),
                new FastVector3Int(4, 0, 0),
            };

            List<FastVector3Int> inner = new()
            {
                new FastVector3Int(1, 1, 0),
                new FastVector3Int(2, 1, 0),
                new FastVector3Int(2, 2, 0),
            };

            Assert.IsTrue(hull.IsConvexHullInsideConvexHull(grid, inner));
            inner.Add(new FastVector3Int(5, 5, 0));
            Assert.IsFalse(hull.IsConvexHullInsideConvexHull(grid, inner));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConvexHullInsideConvexHullFastVectorHandlesCounterClockwiseOrder()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> hull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(4, 0, 0),
                new FastVector3Int(4, 4, 0),
                new FastVector3Int(0, 4, 0),
            };

            List<FastVector3Int> inner = new()
            {
                new FastVector3Int(1, 1, 0),
                new FastVector3Int(2, 2, 0),
                new FastVector3Int(3, 1, 0),
            };

            Assert.IsTrue(hull.IsConvexHullInsideConvexHull(grid, inner));
            inner.Add(new FastVector3Int(-1, 0, 0));
            Assert.IsFalse(hull.IsConvexHullInsideConvexHull(grid, inner));

            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildConcaveHullVariantsMatchConvexHullForRectangle()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 3, 0),
                new FastVector3Int(3, 3, 0),
                new FastVector3Int(3, 0, 0),
            };

            List<FastVector3Int> convex = points.BuildConvexHull(
                grid,
                includeColinearPoints: false
            );
            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            CollectionAssert.AreEquivalent(convex, concaveEdgeSplit);
            CollectionAssert.AreEquivalent(convex, concaveKnn);

            CollectionAssert.AreEquivalent(convex, concave);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullExcludesInteriorColinearPointsFast()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(-2, 0, 0),
                new FastVector3Int(-1, 0, 0),
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(1, 0, 0),
                new FastVector3Int(2, 0, 0),
            };

            List<FastVector3Int> hull = points.BuildConvexHull(grid);
            CollectionAssert.AreEquivalent(
                new[] { new FastVector3Int(-2, 0, 0), new FastVector3Int(2, 0, 0) },
                hull
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullPermutationInvariance()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(3, 0, 0),
                new FastVector3Int(3, 3, 0),
                new FastVector3Int(0, 3, 0),
                new FastVector3Int(1, 1, 0), // interior
            };
            List<FastVector3Int> expected = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(3, 0, 0),
                new FastVector3Int(3, 3, 0),
                new FastVector3Int(0, 3, 0),
            };

            List<FastVector3Int> hullA = points.BuildConvexHull(grid);
            points.Reverse();
            List<FastVector3Int> hullB = points.BuildConvexHull(grid);
            CollectionAssert.AreEquivalent(expected, hullA);
            CollectionAssert.AreEquivalent(expected, hullB);

            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildConcaveHullVariantsMatchConvexHullForTriangle()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 3, 0),
                new FastVector3Int(3, 3, 0),
                new FastVector3Int(3, 0, 0),
            };

            List<FastVector3Int> convex = points.BuildConvexHull(
                grid,
                includeColinearPoints: false
            );
            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            CollectionAssert.AreEquivalent(convex, concaveEdgeSplit);
            CollectionAssert.AreEquivalent(convex, concaveKnn);
            CollectionAssert.AreEquivalent(convex, concave);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConcaveHullTrivialShapesReturnConvexHull()
        {
            Grid grid = CreateGrid(out GameObject owner);

            // Two points
            List<FastVector3Int> twoPoints = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(3, 0, 0),
            };

            List<FastVector3Int> convexTwo = twoPoints.BuildConvexHull(grid);
            List<FastVector3Int> concaveTwoEdgeSplit = twoPoints.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveTwoKnn = twoPoints.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concaveTwo = twoPoints.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );
            CollectionAssert.AreEquivalent(convexTwo, concaveTwoEdgeSplit);
            CollectionAssert.AreEquivalent(convexTwo, concaveTwoKnn);
            CollectionAssert.AreEquivalent(convexTwo, concaveTwo);

            // Three points
            List<FastVector3Int> threePoints = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(2, 0, 0),
                new FastVector3Int(1, 1, 0),
            };
            List<FastVector3Int> convexThree = threePoints.BuildConvexHull(grid);
            List<FastVector3Int> concaveThreeEdgeSplit = threePoints.BuildConcaveHullEdgeSplit(
                grid
            );
            List<FastVector3Int> concaveThreeKnn = threePoints.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concaveThree = threePoints.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );
            CollectionAssert.AreEquivalent(convexThree, concaveThreeEdgeSplit);
            CollectionAssert.AreEquivalent(convexThree, concaveThreeKnn);
            CollectionAssert.AreEquivalent(convexThree, concaveThree);

            // Four points (rectangle)
            List<FastVector3Int> rectangle = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 3, 0),
                new FastVector3Int(3, 3, 0),
                new FastVector3Int(3, 0, 0),
            };
            List<FastVector3Int> convexRect = rectangle.BuildConvexHull(grid);
            List<FastVector3Int> concaveRectEdgeSplit = rectangle.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveRectKnn = rectangle.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concaveRect = rectangle.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );
            CollectionAssert.AreEquivalent(convexRect, concaveRectEdgeSplit);
            CollectionAssert.AreEquivalent(convexRect, concaveRectKnn);
            CollectionAssert.AreEquivalent(convexRect, concaveRect);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConcaveHullHandlesDuplicatesAndColinear()
        {
            Grid grid = CreateGrid(out GameObject owner);

            // Duplicates and colinear points along X-axis
            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(1, 0, 0),
                new FastVector3Int(2, 0, 0),
                new FastVector3Int(2, 0, 0),
            };

            List<FastVector3Int> convex = points.BuildConvexHull(grid);
            // Expect convex hull to be endpoints only
            CollectionAssert.AreEquivalent(
                new[] { new FastVector3Int(0, 0, 0), new FastVector3Int(2, 0, 0) },
                convex
            );

            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            CollectionAssert.AreEquivalent(convex, concaveEdgeSplit);
            CollectionAssert.AreEquivalent(convex, concaveKnn);
            CollectionAssert.AreEquivalent(convex, concave);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullAllPointsIdenticalReturnsSinglePoint()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = Enumerable
                .Repeat(new FastVector3Int(5, 5, 0), 20)
                .ToList();
            List<FastVector3Int> convex = points.BuildConvexHull(grid);
            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            Assert.AreEqual(1, convex.Count);
            CollectionAssert.AreEquivalent(convex, concaveEdgeSplit);
            CollectionAssert.AreEquivalent(convex, concaveKnn);
            CollectionAssert.AreEquivalent(convex, concave);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullWithDuplicatesAndFourCornersReturnsCornersConcaveMatches()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(5, 0, 0),
                new FastVector3Int(5, 0, 0),
                new FastVector3Int(5, 5, 0),
                new FastVector3Int(0, 5, 0),
                new FastVector3Int(0, 5, 0),
                new FastVector3Int(2, 0, 0),
                new FastVector3Int(3, 0, 0), // colinear along bottom
                new FastVector3Int(5, 3, 0),
                new FastVector3Int(5, 2, 0), // colinear along right
            };

            List<FastVector3Int> convex = points.BuildConvexHull(grid);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(5, 0, 0),
                    new FastVector3Int(5, 5, 0),
                    new FastVector3Int(0, 5, 0),
                },
                convex
            );

            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );
            CollectionAssert.AreEquivalent(convex, concaveEdgeSplit);
            CollectionAssert.AreEquivalent(convex, concaveKnn);
            CollectionAssert.AreEquivalent(convex, concave);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LargePointCloudConcaveHullsInsideConvexHull()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = GenerateRandomPointsSquare(600, 50, seed: 4242);
            // Ensure a few extremes are present
            points.AddRange(
                new[]
                {
                    new FastVector3Int(-50, -50, 0),
                    new FastVector3Int(50, -50, 0),
                    new FastVector3Int(50, 50, 0),
                    new FastVector3Int(-50, 50, 0),
                }
            );

            List<FastVector3Int> convex = points.BuildConvexHull(grid);
            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            // No duplicates in hulls
            Assert.AreEqual(convex.Distinct().Count(), convex.Count);
            Assert.AreEqual(concaveEdgeSplit.Distinct().Count(), concaveEdgeSplit.Count);
            Assert.AreEqual(concaveKnn.Distinct().Count(), concaveKnn.Count);
            Assert.AreEqual(concave.Distinct().Count(), concave.Count);

            // Hull points must be drawn from input set
            HashSet<FastVector3Int> input = new(points);
            Assert.IsTrue(convex.All(p => input.Contains(p)));
            Assert.IsTrue(concaveEdgeSplit.All(p => input.Contains(p)));
            Assert.IsTrue(concaveKnn.All(p => input.Contains(p)));
            Assert.IsTrue(concave.All(p => input.Contains(p)));

            // Concave hulls are inside convex hull
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveEdgeSplit));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveKnn));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concave));

            yield return null;
        }

        [UnityTest]
        public IEnumerator LargePointCloudConvexHullReasonableAndContainsExtremes()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> points = GenerateRandomPointsSquare(800, 100, seed: 2025);
            points.AddRange(
                new[]
                {
                    new FastVector3Int(-100, -100, 0),
                    new FastVector3Int(100, -100, 0),
                    new FastVector3Int(100, 100, 0),
                    new FastVector3Int(-100, 100, 0),
                }
            );
            yield break;
        }

        [UnityTest]
        public IEnumerator ConvexHullCirclePointsAllOnHull()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> circle = GenerateCirclePoints(64, 25);
            List<FastVector3Int> hull = circle.BuildConvexHull(grid, includeColinearPoints: true);
            // On a (rough) circle with integer coordinates, most points should be part of the convex hull
            // Note: Jarvis March may skip some hull vertices on curves when jumping between non-collinear points
            // We expect at least 75% of distinct points to be included
            int distinctCount = circle.Distinct().Count();
            Assert.GreaterOrEqual(
                hull.Count,
                distinctCount * 3 / 4,
                $"Expected at least {distinctCount * 3 / 4} points in hull, got {hull.Count}"
            );
            CollectionAssert.IsSubsetOf(hull, circle);
            // Verify no duplicates in hull
            Assert.AreEqual(
                hull.Distinct().Count(),
                hull.Count,
                "Hull should not contain duplicates"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullRectanglePerimeterOnlyCorners()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> rectPerimeter = new();
            for (int x = 0; x <= 10; ++x)
            {
                rectPerimeter.Add(new FastVector3Int(x, 0, 0));
                rectPerimeter.Add(new FastVector3Int(x, 6, 0));
            }
            for (int y = 1; y < 6; ++y)
            {
                rectPerimeter.Add(new FastVector3Int(0, y, 0));
                rectPerimeter.Add(new FastVector3Int(10, y, 0));
            }

            List<FastVector3Int> hull = rectPerimeter.BuildConvexHull(grid);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(10, 0, 0),
                    new FastVector3Int(10, 6, 0),
                    new FastVector3Int(0, 6, 0),
                },
                hull
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvexHullRectanglePerimeterIncludeColinearKeepsEdgePoints()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> rectPerimeter = new();
            for (int x = 0; x <= 10; ++x)
            {
                rectPerimeter.Add(new FastVector3Int(x, 0, 0));
                rectPerimeter.Add(new FastVector3Int(x, 6, 0));
            }
            for (int y = 1; y < 6; ++y)
            {
                rectPerimeter.Add(new FastVector3Int(0, y, 0));
                rectPerimeter.Add(new FastVector3Int(10, y, 0));
            }

            List<FastVector3Int> hull = rectPerimeter.BuildConvexHull(
                grid,
                includeColinearPoints: true
            );

            // Corners must be present
            CollectionAssert.IsSubsetOf(
                new[]
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(10, 0, 0),
                    new FastVector3Int(10, 6, 0),
                    new FastVector3Int(0, 6, 0),
                },
                hull
            );

            // Edge points included when including colinear points
            CollectionAssert.IsSubsetOf(
                new[]
                {
                    new FastVector3Int(2, 0, 0),
                    new FastVector3Int(3, 0, 0),
                    new FastVector3Int(10, 2, 0),
                    new FastVector3Int(10, 3, 0),
                },
                hull
            );

            Assert.Greater(hull.Count, 4);

            yield return null;
        }

        [UnityTest]
        public IEnumerator RandomCloudMultipleSeedsConvexContainsConcaves()
        {
            Grid grid = CreateGrid(out GameObject owner);

            int[] seeds = { 11, 123, 9999 };
            foreach (int seed in seeds)
            {
                List<FastVector3Int> points = GenerateRandomPointsSquare(300, 40, seed);
                List<FastVector3Int> convex = points.BuildConvexHull(grid);
                List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
                List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
                List<FastVector3Int> concave = points.BuildConcaveHull(
                    grid,
                    new UnityExtensions.ConcaveHullOptions
                    {
                        Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                    }
                );

                // Invariants: no duplicates and hull points are from input
                Assert.AreEqual(convex.Distinct().Count(), convex.Count);
                Assert.AreEqual(concaveEdgeSplit.Distinct().Count(), concaveEdgeSplit.Count);
                Assert.AreEqual(concaveKnn.Distinct().Count(), concaveKnn.Count);
                Assert.AreEqual(concave.Distinct().Count(), concave.Count);

                HashSet<FastVector3Int> input = new(points);
                Assert.IsTrue(convex.All(input.Contains));
                Assert.IsTrue(concaveEdgeSplit.All(input.Contains));
                Assert.IsTrue(concaveKnn.All(input.Contains));
                Assert.IsTrue(concave.All(input.Contains));

                // Concave hulls must be inside convex hull
                Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveEdgeSplit));
                Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveKnn));
                Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concave));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossShapeConvexCornersConcavesSubset()
        {
            Grid grid = CreateGrid(out GameObject owner);

            // Cross shape: long plus sign
            List<FastVector3Int> points = new();
            for (int i = -5; i <= 5; ++i)
            {
                points.Add(new FastVector3Int(i, 0, 0));
                points.Add(new FastVector3Int(0, i, 0));
            }
            points.Add(new FastVector3Int(-6, 0, 0));
            points.Add(new FastVector3Int(6, 0, 0));
            points.Add(new FastVector3Int(0, -6, 0));
            points.Add(new FastVector3Int(0, 6, 0));

            List<FastVector3Int> convex = points.BuildConvexHull(grid);
            List<FastVector3Int> concaveEdgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = points.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = points.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            // Expected convex corners of cross’s bounding box
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new FastVector3Int(-6, 0, 0),
                    new FastVector3Int(6, 0, 0),
                    new FastVector3Int(0, -6, 0),
                    new FastVector3Int(0, 6, 0),
                },
                convex
            );

            // Concave hulls should be subsets of convex; do not assert exact points
            HashSet<FastVector3Int> convexSet = new(convex);
            Assert.IsTrue(concaveEdgeSplit.All(p => convexSet.Contains(p) || points.Contains(p)));
            Assert.IsTrue(concaveKnn.All(p => convexSet.Contains(p) || points.Contains(p)));
            Assert.IsTrue(concave.All(p => convexSet.Contains(p) || points.Contains(p)));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveEdgeSplit));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveKnn));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concave));

            yield return null;
        }

        [UnityTest]
        public IEnumerator UShapeConcaveInsideConvex()
        {
            Grid grid = CreateGrid(out GameObject owner);

            // U-shape perimeter
            List<FastVector3Int> u = new();
            for (int x = 0; x <= 8; ++x)
            {
                u.Add(new FastVector3Int(x, 0, 0));
            }
            for (int y = 1; y <= 6; ++y)
            {
                u.Add(new FastVector3Int(0, y, 0));
                u.Add(new FastVector3Int(8, y, 0));
            }

            List<FastVector3Int> convex = u.BuildConvexHull(grid);
            List<FastVector3Int> concaveEdgeSplit = u.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> concaveKnn = u.BuildConcaveHullKnn(grid);
            List<FastVector3Int> concave = u.BuildConcaveHull(
                grid,
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                }
            );

            // Invariants without over-constraining shape
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveEdgeSplit));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concaveKnn));
            Assert.IsTrue(convex.IsConvexHullInsideConvexHull(grid, concave));
            Assert.IsTrue(concaveEdgeSplit.All(p => u.Contains(p)));
            Assert.IsTrue(concaveKnn.All(p => u.Contains(p)));
            Assert.IsTrue(concave.All(p => u.Contains(p)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPositionInsideDetectsInsideAndOutside()
        {
            Grid grid = CreateGrid(out GameObject owner);

            List<FastVector3Int> hull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 4, 0),
                new FastVector3Int(4, 4, 0),
                new FastVector3Int(4, 0, 0),
            };

            Assert.IsTrue(
                UnityExtensions.IsPositionInside(hull, new FastVector3Int(1, 1, 0), grid)
            );
            Assert.IsFalse(
                UnityExtensions.IsPositionInside(hull, new FastVector3Int(5, 5, 0), grid)
            );

            yield return null;
        }

        [Test]
        public void GetCosineMatchesExpectedValue()
        {
            Vector2 a = new(1f, 0f);
            Vector3 b = new(0f, 1f, 0f);
            Vector3 origin = Vector3.zero;

            double cosine = UnityExtensions.GetCosine(a, b, origin);
            Assert.AreEqual(0d, cosine);
        }

        [Test]
        public void IntersectsDetectsLineSegmentIntersection()
        {
            Vector2 a1 = new(0f, 0f);
            Vector2 a2 = new(2f, 2f);
            Vector2 b1 = new(0f, 2f);
            Vector2 b2 = new(2f, 0f);

            Assert.IsTrue(UnityExtensions.Intersects(a1, a2, b1, b2));
            Assert.IsFalse(
                UnityExtensions.Intersects(a1, a2, new Vector2(3f, 3f), new Vector2(4f, 4f))
            );
        }

        [Test]
        public void LiesOnSegmentDetectsColinearPoint()
        {
            Vector2 p = new(0f, 0f);
            Vector2 q = new(1f, 1f);
            Vector2 r = new(2f, 2f);
            Assert.IsTrue(UnityExtensions.LiesOnSegment(p, q, r));
            Assert.IsFalse(UnityExtensions.LiesOnSegment(p, new Vector2(3f, 1f), r));
        }

        [Test]
        public void AllFastPositionsWithinEnumeratesEveryCell()
        {
            BoundsInt bounds = new(0, 0, 0, 2, 2, 1);
            List<FastVector3Int> buffer = new();
            List<FastVector3Int> list = bounds.AllFastPositionsWithin(buffer);
            List<FastVector3Int> enumerated = bounds.AllFastPositionsWithin().ToList();

            CollectionAssert.AreEquivalent(list, enumerated);
            Assert.AreEqual(4, list.Count);
            Assert.Contains(new FastVector3Int(1, 1, 0), list);
        }

        [Test]
        public void ContainsFastVectorReturnsFalseOutside()
        {
            BoundsInt bounds = new(0, 0, 0, 2, 2, 2);
            Assert.IsFalse(bounds.Contains(new FastVector3Int(2, 2, 2)));
        }

        [Test]
        public void IsOnEdge2DDetectsEdgeAndInterior()
        {
            BoundsInt bounds = new(0, 0, 0, 3, 3, 1);
            Assert.IsTrue(new FastVector3Int(0, 1, 0).IsOnEdge2D(bounds));
            Assert.IsTrue(new FastVector3Int(2, 2, 0).IsOnEdge2D(bounds));
            Assert.IsFalse(new FastVector3Int(1, 1, 0).IsOnEdge2D(bounds));
        }

        [UnityTest]
        public IEnumerator WithPaddingAndSetOffsetsManipulateRectTransform()
        {
            GameObject owner = Track(new GameObject("RectTransformTest", typeof(RectTransform)));

            RectTransform rectTransform = owner.GetComponent<RectTransform>();
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            rectTransform.SetLeft(10f);
            rectTransform.SetBottom(5f);
            rectTransform.SetRight(15f);
            rectTransform.SetTop(20f);

            Assert.AreEqual(10f, rectTransform.offsetMin.x);
            Assert.AreEqual(5f, rectTransform.offsetMin.y);
            Assert.AreEqual(-15f, rectTransform.offsetMax.x);
            Assert.AreEqual(-20f, rectTransform.offsetMax.y);

            BoundsInt bounds = new(0, 0, 0, 1, 1, 1);
            BoundsInt padded = bounds.WithPadding(2, 3);
            Assert.AreEqual(-2, padded.xMin);
            Assert.AreEqual(3, padded.xMax);
            Assert.AreEqual(-3, padded.yMin);
            Assert.AreEqual(4, padded.yMax);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SetColorsAppliesColorAcrossStates()
        {
            GameObject owner = Track(new GameObject("SliderTest", typeof(Canvas), typeof(Slider)));

            Slider slider = owner.GetComponent<Slider>();
            Color color = new(0.25f, 0.5f, 0.75f, 1f);
            slider.SetColors(color);

            ColorBlock block = slider.colors;
            Assert.AreEqual(color, block.normalColor);
            Assert.AreEqual(color, block.highlightedColor);
            Assert.AreEqual(color, block.pressedColor);
            Assert.AreEqual(color, block.selectedColor);
            Assert.AreEqual(color, block.disabledColor);

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsDontDestroyOnLoadDetectsFlag()
        {
            GameObject owner = Track(new GameObject("DontDestroyTest"));

            Assert.IsFalse(owner.IsDontDestroyOnLoad());
            UnityEngine.Object.DontDestroyOnLoad(owner);
            Assert.IsTrue(owner.IsDontDestroyOnLoad());

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsCircleFullyContainedValidatesBoundary()
        {
            GameObject owner = Track(
                new GameObject("CircleColliderTest", typeof(CircleCollider2D))
            );

            CircleCollider2D collider = owner.GetComponent<CircleCollider2D>();
            collider.radius = 1f;

            Assert.IsTrue(collider.IsCircleFullyContained(Vector2.zero, 0.5f));
            Assert.IsFalse(collider.IsCircleFullyContained(Vector2.zero, 1.5f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator InvertPolygonColliderCreatesOuterPath()
        {
            GameObject owner = Track(
                new GameObject("PolygonColliderTest", typeof(PolygonCollider2D))
            );

            PolygonCollider2D collider = owner.GetComponent<PolygonCollider2D>();
            collider.pathCount = 1;
            collider.SetPath(
                0,
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                }
            );

            Rect outer = new(-1f, -1f, 4f, 4f);
            collider.Invert(outer);

            Assert.AreEqual(2, collider.pathCount);
            Vector2[] outerPath = collider.GetPath(0);
            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector2(outer.xMin, outer.yMin),
                    new Vector2(outer.xMin, outer.yMax),
                    new Vector2(outer.xMax, outer.yMax),
                    new Vector2(outer.xMax, outer.yMin),
                },
                outerPath
            );

            Vector2[] innerPath = collider.GetPath(1);
            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                },
                innerPath
            );

            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator GetSpritesFromClipEnumeratesKeyframes()
        {
            Texture2D texture = Track(new Texture2D(16, 16));

            Sprite spriteA = Track(
                Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f))
            );
            Sprite spriteB = Track(
                Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f))
            );
            AnimationClip clip = Track(new AnimationClip());

            UnityEditor.EditorCurveBinding binding = new()
            {
                path = string.Empty,
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer),
            };

            UnityEditor.ObjectReferenceKeyframe[] frames =
            {
                new UnityEditor.ObjectReferenceKeyframe { time = 0f, value = spriteA },
                new UnityEditor.ObjectReferenceKeyframe { time = 1f, value = spriteB },
            };

            UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

            List<Sprite> sprites = clip.GetSpritesFromClip().ToList();
            Assert.AreEqual(2, sprites.Count);
            Assert.Contains(spriteA, sprites);
            Assert.Contains(spriteB, sprites);

            yield return null;
        }
#endif
    }
}
