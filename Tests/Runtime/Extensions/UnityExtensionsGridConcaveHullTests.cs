#if UNITY_EDITOR || UNITY_INCLUDE_TESTS || WALLSTOP_CONCAVE_HULL_STATS
#define ENABLE_CONCAVE_HULL_STATS
#endif

namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;

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

        private static IEnumerable<TestCaseData> GridAxisCornerScenarioCases()
        {
            yield return new TestCaseData(
                "StraightFallbackHallway",
                CreateStraightFallbackPoints(includeInteriorColumn: false),
                8,
                220f,
                5,
                new[] { FV(1, 1), FV(4, 1) },
                0
            ).SetName("ConcaveHullGridRecoversAxisCornersWithStraightFallback");

            yield return new TestCaseData(
                "AxisPathHallway",
                CreateStraightFallbackPoints(includeInteriorColumn: true),
                8,
                220f,
                5,
                new[] { FV(1, 1), FV(4, 1) },
                1
            ).SetName("ConcaveHullGridRecoversAxisCornersWithAxisPath");
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

        [TestCaseSource(nameof(GridAxisCornerScenarioCases))]
        public void ConcaveHullAxisCornerRepairDiagnostics(
            string label,
            List<FastVector3Int> points,
            int bucketSize,
            float angleThreshold,
            int nearestNeighbors,
            FastVector3Int[] requiredCorners,
            int expectedAxisPathInsertionsMin
        )
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            UnityExtensions.ConcaveHullOptions edgeSplitOptions =
                new UnityExtensions.ConcaveHullOptions
                {
                    Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                    BucketSize = bucketSize,
                    AngleThreshold = angleThreshold,
                };

            List<FastVector3Int> edgeSplit = points.BuildConcaveHull(grid, edgeSplitOptions);
            AssertRequiredVertices($"{label} edge-split", requiredCorners, edgeSplit);
#if ENABLE_CONCAVE_HULL_STATS
            UnityExtensions.ConcaveHullRepairStats edgeStats =
                UnityExtensions.ProfileConcaveHullRepair(
                    edgeSplit,
                    points,
                    UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                    angleThreshold
                );
            Assert.AreEqual(
                0,
                edgeStats.DuplicateRemovals,
                $"{label} edge-split should not emit duplicates."
            );
            if (expectedAxisPathInsertionsMin == 0)
            {
                Assert.AreEqual(
                    0,
                    edgeStats.AxisPathInsertions,
                    $"{label} edge-split should rely on straight fallback without BFS inserts."
                );
            }
            else
            {
                Assert.GreaterOrEqual(
                    edgeStats.AxisPathInsertions,
                    expectedAxisPathInsertionsMin,
                    $"{label} edge-split should reinstate missing corners via BFS axis path."
                );
            }
#endif

            List<FastVector3Int> knn = points.BuildConcaveHullKnn(grid, nearestNeighbors);
            AssertRequiredVertices($"{label} knn", requiredCorners, knn);
        }

        [Test]
        public void ConcaveHullRepairHandlesPermutedLShapes()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> basePoints = CreatePointList(
                (0, 0),
                (0, 4),
                (1, 4),
                (1, 3),
                (2, 3),
                (2, 2),
                (3, 2),
                (3, 1),
                (4, 1),
                (4, 0)
            );
            FastVector3Int[] required = { FV(1, 3), FV(2, 2), FV(3, 1) };
            IRandom random = new PcgRandom(0xC0FFEE);
            for (int iteration = 0; iteration < 24; ++iteration)
            {
                List<FastVector3Int> permuted = basePoints.OrderBy(_ => random.Next()).ToList();
                List<FastVector3Int> hull = permuted.BuildConcaveHullEdgeSplit(
                    grid,
                    bucketSize: 8,
                    angleThreshold: 220f
                );
                AssertRequiredVertices($"Permuted L iteration {iteration}", required, hull);
#if ENABLE_CONCAVE_HULL_STATS
                UnityExtensions.ConcaveHullRepairStats stats =
                    UnityExtensions.ProfileConcaveHullRepair(
                        hull,
                        permuted,
                        UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                        220f
                    );
                Assert.AreEqual(
                    0,
                    stats.DuplicateRemovals,
                    $"Permuted L iteration {iteration} should not emit duplicates."
                );
                Assert.LessOrEqual(
                    stats.AxisPathInsertions,
                    12,
                    $"Permuted L iteration {iteration} should only require a few axis-path inserts."
                );
#endif
            }
        }

        [TestCaseSource(nameof(AxisCornerCases))]
        public void ConcaveHullAxisCornerDiagnostics(
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

            foreach (FastVector3Int required in requiredCorners)
            {
                if (!edgeSplit.Contains(required))
                {
                    Debug.LogError(
                        $"[AxisCornerDiagnostics] {label} missing {required}. Hull vertices:\n{string.Join(", ", edgeSplit)}"
                    );
                }
            }
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

        [Test]
        public void EdgeSplitFallsBackToConvexWhenAngleThresholdLow()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> concaveShape = CreatePointList(
                (0, 0),
                (0, 4),
                (2, 4),
                (2, 2),
                (4, 2),
                (4, 0)
            );
            FastVector3Int elbow = FV(2, 2);

            List<FastVector3Int> convex = concaveShape.BuildConvexHull(
                grid,
                includeColinearPoints: false
            );
            List<FastVector3Int> hull = concaveShape.BuildConcaveHullEdgeSplit(
                grid,
                bucketSize: 8,
                angleThreshold: 30f
            );

            CollectionAssert.AreEquivalent(
                convex,
                hull,
                "Low angle thresholds should force edge-split hulls to align with the convex hull."
            );
            Assert.IsFalse(
                hull.Contains(elbow),
                "Elbow should not be present once the algorithm falls back to the convex hull."
            );
        }

        [Test]
        public void EdgeSplitCapturesConcaveVerticesWhenAngleThresholdHigh()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> concaveShape = CreatePointList(
                (0, 0),
                (0, 4),
                (2, 4),
                (2, 2),
                (4, 2),
                (4, 0)
            );
            FastVector3Int elbow = FV(2, 2);

            List<FastVector3Int> hull = concaveShape.BuildConcaveHullEdgeSplit(
                grid,
                bucketSize: 8,
                angleThreshold: 240f
            );

            AssertHullSubset(concaveShape, hull);
            Assert.IsTrue(
                hull.Contains(elbow),
                "High angle thresholds should reintroduce the concave elbow."
            );
        }

        private static IEnumerable<TestCaseData> EdgeSplitBucketCases()
        {
            yield return new TestCaseData(1, false).SetName(
                "EdgeSplitFallsBackWhenBucketSizeTooSmall"
            );
            yield return new TestCaseData(16, true).SetName(
                "EdgeSplitCapturesConcavityWhenBucketSizeAdequate"
            );
        }

        [TestCaseSource(nameof(EdgeSplitBucketCases))]
        public void EdgeSplitBucketSizeControlsConcavity(int bucketSize, bool expectElbow)
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> concaveShape = CreatePointList(
                (0, 0),
                (0, 4),
                (2, 4),
                (2, 2),
                (4, 2),
                (4, 0)
            );
            FastVector3Int elbow = FV(2, 2);

            List<FastVector3Int> hull = concaveShape.BuildConcaveHullEdgeSplit(
                grid,
                bucketSize: bucketSize,
                angleThreshold: 240f
            );

            AssertHullSubset(concaveShape, hull);
            bool containsElbow = hull.Contains(elbow);
            TestContext.WriteLine(
                $"BucketSize={bucketSize}, ExpectedElbow={expectElbow}, ActualElbow={containsElbow}, HullVertices={hull.Count}"
            );

            string expectation = expectElbow
                ? "Adequate bucket sizes should feed enough candidates to reintroduce concave vertices."
                : "Small bucket sizes should starve the QuadTree and fall back to convex hull behaviour.";
            Assert.AreEqual(expectElbow, containsElbow, expectation);
        }

        [Test]
        public void ConcaveHullRepairMetricsRemainBoundedOnLargeSamples()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> samples = new();
            for (int y = 0; y < 120; ++y)
            {
                for (int x = 0; x < 120; ++x)
                {
                    // Carve out a concave cavity to force the repair path.
                    if (x > 30 && x < 90 && y > 30 && y < 90)
                    {
                        continue;
                    }
                    samples.Add(new FastVector3Int(x, y, 0));
                }
            }

            Assert.GreaterOrEqual(samples.Count, 10000);

            UnityExtensions.ConcaveHullOptions options = new UnityExtensions.ConcaveHullOptions
            {
                Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                BucketSize = 48,
                AngleThreshold = 220f,
            };

            List<FastVector3Int> hull = samples.BuildConcaveHull(grid, options);
            UnityExtensions.ConcaveHullRepairStats stats = UnityExtensions.ProfileConcaveHullRepair(
                hull,
                samples,
                UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                options.AngleThreshold
            );

            TestContext.WriteLine(
                $"Repair stats: start={stats.StartHullCount}, final={stats.FinalHullCount}, axisCorners={stats.AxisCornerInsertions}, axisPaths={stats.AxisPathInsertions}, duplicates={stats.DuplicateRemovals}, candidates={stats.CandidateConnections}, frontier={stats.MaxFrontierSize}"
            );

            Assert.Greater(
                stats.AxisCornerInsertions + stats.AxisPathInsertions,
                0,
                "Repair should have inserted additional axis corners for the carved cavity."
            );
            Assert.LessOrEqual(
                stats.FinalHullCount,
                stats.OriginalPointsCount,
                "Repair must not exceed the source point budget."
            );
            Assert.AreEqual(0, stats.DuplicateRemovals, "Repair should deduplicate as it goes.");
            Assert.Greater(stats.MaxFrontierSize, 0, "BFS frontier should have processed nodes.");
        }

        [Test]
        public void ConcaveHullRepairIsIdempotentAfterAxisCornersInserted()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> samples = CreatePointList(
                (0, 0),
                (0, 3),
                (1, 3),
                (1, 2),
                (2, 2),
                (2, 1),
                (3, 1),
                (3, 0)
            );

            UnityExtensions.ConcaveHullOptions options = new UnityExtensions.ConcaveHullOptions
            {
                Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                BucketSize = 8,
                AngleThreshold = 220f,
            };

            List<FastVector3Int> repairedHull = samples.BuildConcaveHull(grid, options);
            UnityExtensions.ConcaveHullRepairStats stats = UnityExtensions.ProfileConcaveHullRepair(
                new List<FastVector3Int>(repairedHull),
                new List<FastVector3Int>(samples),
                options.Strategy,
                options.AngleThreshold
            );

            Assert.AreEqual(
                0,
                stats.AxisCornerInsertions + stats.AxisPathInsertions,
                "Repair should be a no-op once all axis corners already exist."
            );
            Assert.AreEqual(
                0,
                stats.DuplicateRemovals,
                "Re-running repair on an axis-aligned hull must not create duplicates."
            );
            Assert.AreEqual(
                repairedHull.Count,
                stats.FinalHullCount,
                "Axis-aligned hulls should retain their vertex count across repair passes."
            );
        }

        [Test]
        public void ConcaveHullRepairMetricsRemainBoundedAcrossMultipleCavities()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> samples = new();
            for (int y = 0; y < 150; ++y)
            {
                for (int x = 0; x < 150; ++x)
                {
                    bool inFirstCavity = x > 25 && x < 55 && y > 25 && y < 55;
                    bool inSecondCavity = x > 95 && x < 125 && y > 70 && y < 120;
                    if (inFirstCavity || inSecondCavity)
                    {
                        continue;
                    }
                    samples.Add(new FastVector3Int(x, y, 0));
                }
            }

            UnityExtensions.ConcaveHullOptions options = new UnityExtensions.ConcaveHullOptions
            {
                Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                BucketSize = 64,
                AngleThreshold = 240f,
            };

            List<FastVector3Int> hull = samples.BuildConcaveHull(grid, options);
#if ENABLE_CONCAVE_HULL_STATS
            UnityExtensions.ConcaveHullRepairStats stats = UnityExtensions.ProfileConcaveHullRepair(
                hull,
                samples,
                UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                options.AngleThreshold
            );

            Assert.Greater(
                stats.AxisPathInsertions + stats.AxisCornerInsertions,
                0,
                "Repair should reintroduce axis corners for each cavity."
            );
            Assert.AreEqual(
                0,
                stats.DuplicateRemovals,
                "Repair should deduplicate as it goes (multi-cavity)."
            );
            Assert.LessOrEqual(
                stats.FinalHullCount,
                stats.OriginalPointsCount,
                "Repair must stay within the original point budget for multi-cavity datasets."
            );
#else
            Assert.IsNotNull(hull);
#endif
        }

        [Test]
        public void ConcaveHullRepairHandlesSharedThroatCavities()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> samples = CreateSharedThroatSamples();
            UnityExtensions.ConcaveHullOptions options = new UnityExtensions.ConcaveHullOptions
            {
                Strategy = UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                BucketSize = 32,
                AngleThreshold = 240f,
            };

            List<FastVector3Int> hull = samples.BuildConcaveHull(grid, options);
            AssertHullSubset(samples, hull);
#if ENABLE_CONCAVE_HULL_STATS
            UnityExtensions.ConcaveHullRepairStats stats = UnityExtensions.ProfileConcaveHullRepair(
                hull,
                samples,
                UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                options.AngleThreshold
            );
            Assert.Greater(
                stats.AxisPathInsertions + stats.AxisCornerInsertions,
                0,
                "Shared throat cavities should require at least one repair insert."
            );
            Assert.AreEqual(
                0,
                stats.DuplicateRemovals,
                "Shared throat repair should not create duplicates."
            );
#endif
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

        private static List<FastVector3Int> CreateSharedThroatSamples()
        {
            List<FastVector3Int> samples = new();
            for (int y = 0; y < 30; ++y)
            {
                for (int x = 0; x < 30; ++x)
                {
                    bool leftCavity = x >= 5 && x <= 10 && y >= 5 && y <= 20;
                    bool rightCavity = x >= 19 && x <= 24 && y >= 9 && y <= 24;
                    bool sharedThroat = x == 15 && y >= 10 && y <= 14;
                    if ((leftCavity || rightCavity) && !sharedThroat)
                    {
                        continue;
                    }

                    samples.Add(new FastVector3Int(x, y, 0));
                }
            }

            return samples;
        }

        private static List<FastVector3Int> CreateStraightFallbackPoints(bool includeInteriorColumn)
        {
            List<FastVector3Int> points = new()
            {
                FV(0, 0),
                FV(0, 5),
                FV(5, 5),
                FV(5, 0),
                FV(1, 5),
                FV(4, 5),
                FV(4, 0),
                FV(1, 0),
                FV(5, 1),
                FV(0, 1),
                FV(1, 1),
                FV(4, 1),
            };

            if (includeInteriorColumn)
            {
                points.AddRange(CreatePointList((1, 2), (1, 3), (1, 4), (4, 2), (4, 3), (4, 4)));
            }

            return points;
        }
    }
}
