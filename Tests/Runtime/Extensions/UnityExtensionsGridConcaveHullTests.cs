namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class UnityExtensionsGridConcaveHullTests : GridTestBase
    {
        private static FastVector3Int FV(int x, int y)
        {
            return new FastVector3Int(x, y, 0);
        }

        [Test]
        public void BuildConcaveHullEdgeSplitMatchesConvexHullForRectangle()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> rectangle = CreatePointList((0, 0), (0, 3), (4, 3), (4, 0));

            List<FastVector3Int> convex = rectangle.BuildConvexHull(
                grid,
                includeColinearPoints: false
            );
            List<FastVector3Int> hull = rectangle.BuildConcaveHullEdgeSplit(grid);

            AssertHullSubset(rectangle, hull);
            CollectionAssert.AreEquivalent(convex, hull);
        }

        [Test]
        public void BuildConcaveHullKnnCapturesConcaveVertices()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> concaveLShape = CreatePointList(
                (0, 0),
                (0, 4),
                (2, 4),
                (2, 2),
                (4, 2),
                (4, 0)
            );
            FastVector3Int elbow = FV(2, 2);

            List<FastVector3Int> hull = concaveLShape.BuildConcaveHullKnn(
                grid,
                nearestNeighbors: 3
            );

            AssertHullSubset(concaveLShape, hull);
            Assert.IsTrue(
                hull.Contains(elbow),
                "KNN concave hull should retain concave vertices when they exist in the point set."
            );
        }

        private static IEnumerable<TestCaseData> ConvexShapeCases()
        {
            yield return new TestCaseData(
                "Rectangle",
                CreatePointList((0, 0), (4, 0), (4, 3), (0, 3))
            ).SetName("ConcaveHullRectangleFallsBackToConvex");
            yield return new TestCaseData(
                "Triangle",
                CreatePointList((0, 0), (3, 0), (2, 4), (1, 2))
            ).SetName("ConcaveHullTriangleFallsBackToConvex");
            yield return new TestCaseData("Line", CreatePointList((0, 0), (0, 5))).SetName(
                "ConcaveHullLineFallsBackToConvex"
            );
        }

        [TestCaseSource(nameof(ConvexShapeCases))]
        public void ConcaveHullFallbacksToConvexForTrivialShapes(
            string label,
            List<FastVector3Int> points
        )
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> convexHull = points.BuildConvexHull(
                grid,
                includeColinearPoints: false
            );
            List<FastVector3Int> edgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> knn = points.BuildConcaveHullKnn(grid);

            CollectionAssert.AreEquivalent(
                convexHull,
                edgeSplit,
                $"{label}: edge-split hull should fall back to convex hull."
            );
            CollectionAssert.AreEquivalent(
                convexHull,
                knn,
                $"{label}: knn hull should fall back to convex hull."
            );
        }

        private static IEnumerable<TestCaseData> ConcaveComparisonCases()
        {
            yield return new TestCaseData(
                "L-Shape",
                CreatePointList((0, 0), (0, 4), (2, 4), (2, 2), (4, 2), (4, 0)),
                240f,
                3,
                new[] { FV(2, 2) },
                new[] { FV(2, 2) }
            ).SetName("ConcaveHullAlgorithmsAgreeLShape");

            yield return new TestCaseData(
                "Staircase",
                CreatePointList((0, 0), (0, 3), (1, 3), (1, 2), (2, 2), (2, 1), (3, 1), (3, 0)),
                200f,
                4,
                new[] { FV(1, 2), FV(2, 1) },
                new[] { FV(1, 2), FV(2, 1) }
            ).SetName("ConcaveHullAlgorithmsAgreeStaircase");
        }

        [TestCaseSource(nameof(ConcaveComparisonCases))]
        public void ConcaveHullAlgorithmsAgreeOnVertices(
            string label,
            List<FastVector3Int> points,
            float angleThreshold,
            int nearestNeighbors,
            FastVector3Int[] requiredEdgeSplit,
            FastVector3Int[] requiredKnn
        )
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> edgeSplit = points.BuildConcaveHullEdgeSplit(
                grid,
                bucketSize: 8,
                angleThreshold: angleThreshold
            );
            List<FastVector3Int> knn = points.BuildConcaveHullKnn(grid, nearestNeighbors);

            AssertHullSubset(points, edgeSplit);
            AssertHullSubset(points, knn);
            AssertRequiredVertices($"{label} edge-split", requiredEdgeSplit, edgeSplit);
            AssertRequiredVertices($"{label} knn", requiredKnn, knn);
        }

        private static IEnumerable<TestCaseData> AxisCornerCases()
        {
            yield return new TestCaseData(
                "StaircaseAxisCorners",
                CreatePointList((0, 0), (0, 3), (1, 3), (1, 2), (2, 2), (2, 1), (3, 1), (3, 0)),
                4,
                new[] { FV(1, 2), FV(2, 1) }
            ).SetName("ConcaveHullPreservesStaircaseCorners");

            yield return new TestCaseData(
                "HorseshoeHallway",
                CreatePointList(
                    (0, 0),
                    (0, 5),
                    (1, 5),
                    (1, 4),
                    (1, 3),
                    (1, 2),
                    (1, 1),
                    (2, 1),
                    (3, 1),
                    (3, 2),
                    (3, 3),
                    (3, 4),
                    (3, 5),
                    (4, 5),
                    (4, 0)
                ),
                5,
                new[] { FV(1, 1), FV(3, 1) }
            ).SetName("ConcaveHullPreservesHorseshoeCorners");

            yield return new TestCaseData(
                "SerpentineCorridor",
                CreatePointList(
                    (0, 0),
                    (0, 4),
                    (1, 4),
                    (1, 3),
                    (2, 3),
                    (2, 4),
                    (3, 4),
                    (3, 3),
                    (3, 2),
                    (3, 1),
                    (3, 0),
                    (2, 0),
                    (2, 1),
                    (1, 1),
                    (1, 0)
                ),
                6,
                new[] { FV(1, 3), FV(2, 1), FV(3, 2) }
            ).SetName("ConcaveHullPreservesSerpentineCorners");
        }

        [TestCaseSource(nameof(AxisCornerCases))]
        public void ConcaveHullPreservesAxisCorners(
            string label,
            List<FastVector3Int> points,
            int nearestNeighbors,
            FastVector3Int[] requiredCorners
        )
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> edgeSplit = points.BuildConcaveHullEdgeSplit(
                grid,
                bucketSize: 8,
                angleThreshold: 200f
            );
            List<FastVector3Int> knn = points.BuildConcaveHullKnn(grid, nearestNeighbors);

            AssertRequiredVertices($"{label} edge-split", requiredCorners, edgeSplit);
            AssertRequiredVertices($"{label} knn", requiredCorners, knn);
        }

        [Test]
        public void ConcaveHullDoesNotInsertDiagonalOnlyCandidates()
        {
            List<FastVector3Int> points = CreatePointList((0, 0), (0, 3), (3, 3), (3, 0), (1, 1));
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> edgeSplit = points.BuildConcaveHullEdgeSplit(grid);
            List<FastVector3Int> knn = points.BuildConcaveHullKnn(grid);

            FastVector3Int diagonal = FV(1, 1);
            Assert.IsFalse(
                edgeSplit.Contains(diagonal),
                "Edge-split hull should not insert diagonal-only points."
            );
            Assert.IsFalse(
                knn.Contains(diagonal),
                "KNN hull should not insert diagonal-only points."
            );
        }

        private static List<FastVector3Int> CreatePointList(params (int x, int y)[] coords)
        {
            return coords.Select(tuple => FV(tuple.x, tuple.y)).ToList();
        }

        private static void AssertHullSubset(
            IReadOnlyCollection<FastVector3Int> source,
            IEnumerable<FastVector3Int> hull
        )
        {
            HashSet<FastVector3Int> sourceSet = new(source);
            foreach (FastVector3Int vertex in hull)
            {
                Assert.IsTrue(
                    sourceSet.Contains(vertex),
                    $"Hull introduced vertex {vertex} that was not part of the input set."
                );
            }
        }

        private static void AssertRequiredVertices(
            string label,
            IEnumerable<FastVector3Int> required,
            IReadOnlyCollection<FastVector3Int> hull
        )
        {
            foreach (FastVector3Int vertex in required)
            {
                Assert.IsTrue(hull.Contains(vertex), $"{label}: hull should contain {vertex}.");
            }
        }
    }
}
