namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Tests.Helper;

    public sealed class UnityExtensionsComprehensiveTests
    {
        private static Grid CreateGrid(out GameObject owner)
        {
            owner = new GameObject("Grid", typeof(Grid));
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

        [Test]
        public void BuildConvexHullVector3IntIncludesColinearPointsWhenRequested()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<Vector3Int> points = new()
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 2, 0),
                    new Vector3Int(2, 2, 0),
                    new Vector3Int(2, 0, 0),
                    new Vector3Int(1, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<Vector3Int> hull = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: true
                );

                CollectionAssert.AreEquivalent(points, hull);
                Assert.AreEqual(new Vector3Int(0, 0, 0), hull[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BuildConvexHullVector3IntExcludesColinearPointsWhenDisabled()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<Vector3Int> points = new()
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 2, 0),
                    new Vector3Int(2, 2, 0),
                    new Vector3Int(2, 0, 0),
                    new Vector3Int(1, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<Vector3Int> hull = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: false
                );

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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BuildConvexHullFastVector3IntProducesExpectedLoop()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<FastVector3Int> points = new()
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 2, 0),
                    new FastVector3Int(2, 2, 0),
                    new FastVector3Int(2, 0, 0),
                    new FastVector3Int(1, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<FastVector3Int> hull = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: false
                );

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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        public void BuildConvexHullFastVector3IntIncludesColinearWhenRequested()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<FastVector3Int> points = new()
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 2, 0),
                    new FastVector3Int(2, 2, 0),
                    new FastVector3Int(2, 0, 0),
                    new FastVector3Int(1, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<FastVector3Int> hull = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: true
                );

                CollectionAssert.AreEquivalent(points, hull);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsPointInsideConvexHullDetectsContainment()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<Vector3Int> hull = new()
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 2, 0),
                    new Vector3Int(2, 2, 0),
                    new Vector3Int(2, 0, 0),
                };

                Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new Vector3Int(1, 1, 0)));
                Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new Vector3Int(3, 3, 0)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsPointInsideConvexHullTreatsBoundaryAsInside()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsPointInsideConvexHullFastVectorDetectsContainment()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<FastVector3Int> hull = new()
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 2, 0),
                    new FastVector3Int(2, 2, 0),
                    new FastVector3Int(2, 0, 0),
                };

                Assert.IsTrue(hull.IsPointInsideConvexHull(grid, new FastVector3Int(1, 1, 0)));
                Assert.IsFalse(hull.IsPointInsideConvexHull(grid, new FastVector3Int(3, 3, 0)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsPointInsideConvexHullFastVectorTreatsBoundaryAsInside()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsConvexHullInsideConvexHullValidatesInteriorPoints()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsConvexHullInsideConvexHullHandlesCounterClockwiseOrder()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsConvexHullInsideConvexHullFastVectorValidatesInteriorPoints()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsConvexHullInsideConvexHullFastVectorHandlesCounterClockwiseOrder()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BuildConcaveHullVariantsMatchConvexHullForRectangle()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<FastVector3Int> points = new()
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 3, 0),
                    new FastVector3Int(3, 3, 0),
                    new FastVector3Int(3, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<FastVector3Int> convex = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: false
                );
                List<FastVector3Int> concave3 = points.BuildConcaveHull3(grid, random);
                List<FastVector3Int> concave2 = points.BuildConcaveHull2(grid, random);
                List<FastVector3Int> concave = points.BuildConcaveHull(grid, random);

                CollectionAssert.AreEquivalent(convex, concave3);
                CollectionAssert.AreEquivalent(convex, concave2);

                CollectionAssert.AreEquivalent(convex, concave);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BuildConcaveHullVariantsMatchConvexHullForTriangle()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
                List<FastVector3Int> points = new()
                {
                    new FastVector3Int(0, 0, 0),
                    new FastVector3Int(0, 3, 0),
                    new FastVector3Int(3, 3, 0),
                    new FastVector3Int(3, 0, 0),
                };

                DeterministicRandom random = new(Array.Empty<double>());
                List<FastVector3Int> convex = points.BuildConvexHull(
                    grid,
                    random,
                    includeColinearPoints: false
                );
                List<FastVector3Int> concave3 = points.BuildConcaveHull3(grid, random);
                List<FastVector3Int> concave2 = points.BuildConcaveHull2(grid, random);
                List<FastVector3Int> concave = points.BuildConcaveHull(grid, random);

                CollectionAssert.AreEquivalent(convex, concave3);
                CollectionAssert.AreEquivalent(convex, concave2);
                CollectionAssert.AreEquivalent(convex, concave);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsPositionInsideDetectsInsideAndOutside()
        {
            Grid grid = CreateGrid(out GameObject owner);
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
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

        [Test]
        public void WithPaddingAndSetOffsetsManipulateRectTransform()
        {
            GameObject owner = new("RectTransformTest", typeof(RectTransform));
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SetColorsAppliesColorAcrossStates()
        {
            GameObject owner = new("SliderTest", typeof(Canvas), typeof(Slider));
            try
            {
                Slider slider = owner.GetComponent<Slider>();
                Color color = new(0.25f, 0.5f, 0.75f, 1f);
                slider.SetColors(color);

                ColorBlock block = slider.colors;
                Assert.AreEqual(color, block.normalColor);
                Assert.AreEqual(color, block.highlightedColor);
                Assert.AreEqual(color, block.pressedColor);
                Assert.AreEqual(color, block.selectedColor);
                Assert.AreEqual(color, block.disabledColor);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsDontDestroyOnLoadDetectsFlag()
        {
            GameObject owner = new("DontDestroyTest");
            try
            {
                Assert.IsFalse(owner.IsDontDestroyOnLoad());
                UnityEngine.Object.DontDestroyOnLoad(owner);
                Assert.IsTrue(owner.IsDontDestroyOnLoad());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void IsCircleFullyContainedValidatesBoundary()
        {
            GameObject owner = new("CircleColliderTest", typeof(CircleCollider2D));
            try
            {
                CircleCollider2D collider = owner.GetComponent<CircleCollider2D>();
                collider.radius = 1f;

                Assert.IsTrue(collider.IsCircleFullyContained(Vector2.zero, 0.5f));
                Assert.IsFalse(collider.IsCircleFullyContained(Vector2.zero, 1.5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void InvertPolygonColliderCreatesOuterPath()
        {
            GameObject owner = new("PolygonColliderTest", typeof(PolygonCollider2D));
            try
            {
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void GetSpritesFromClipEnumeratesKeyframes()
        {
            Texture2D texture = new(16, 16);
            try
            {
                Sprite spriteA = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f)
                );
                Sprite spriteB = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f)
                );
                AnimationClip clip = new();
                try
                {
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
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                    UnityEngine.Object.DestroyImmediate(spriteA);
                    UnityEngine.Object.DestroyImmediate(spriteB);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
#endif
    }
}
