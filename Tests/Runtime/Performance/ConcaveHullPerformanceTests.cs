namespace WallstopStudios.UnityHelpers.Tests.Runtime.Performance
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class ConcaveHullPerformanceTests : CommonTestBase
    {
        private const int DefaultWidth = 16;
        private const int DefaultHeight = 16;
        private const long Vector2AllocationBudgetBytes = 8_192;
        private const long GridAllocationBudgetBytes = 12_288;
        private const int LargeWidth = 128;
        private const int LargeHeight = 128;
        private const long LargeVector2AllocationBudgetBytes = Vector2AllocationBudgetBytes * 8;
        private const long LargeGridAllocationBudgetBytes = GridAllocationBudgetBytes * 8;
        private const int RepairSampleWidth = 160;
        private const int RepairSampleHeight = 160;

        [Test]
        public void BuildConcaveHullKnnVector2AllocationsStayBounded()
        {
            List<Vector2> points = CreateVectorPointCloud(DefaultWidth, DefaultHeight);

            long allocated = GCAssert.MeasureAllocatedBytes(
                () =>
                {
                    List<Vector2> hull = points.BuildConcaveHullKnn(nearestNeighbors: 8);
                    Assert.IsNotNull(hull);
                    Assert.GreaterOrEqual(hull.Count, 3);
                },
                warmupIterations: 3,
                measuredIterations: 1
            );

            Assert.LessOrEqual(
                allocated,
                Vector2AllocationBudgetBytes,
                $"Vector2 concave hull should allocate no more than the hull list (measured {allocated} bytes)."
            );
        }

        [Test]
        public void BuildConcaveHullKnnVector2AllocationsStayBoundedForLargePointCloud()
        {
            List<Vector2> points = CreateVectorPointCloud(LargeWidth, LargeHeight);

            long allocated = GCAssert.MeasureAllocatedBytes(
                () =>
                {
                    List<Vector2> hull = points.BuildConcaveHullKnn(nearestNeighbors: 8);
                    Assert.IsNotNull(hull);
                    Assert.GreaterOrEqual(hull.Count, 3);
                },
                warmupIterations: 2,
                measuredIterations: 1
            );

            Assert.LessOrEqual(
                allocated,
                LargeVector2AllocationBudgetBytes,
                $"Vector2 concave hull (>10k points) should stay within hull list allocations (measured {allocated} bytes)."
            );
        }

        [Test]
        public void BuildConcaveHullKnnGridAllocationsStayBounded()
        {
            List<FastVector3Int> points = CreateGridPointCloud(DefaultWidth, DefaultHeight);
            Grid grid = new GameObject("ConcaveHullGrid").AddComponent<Grid>();
            Track(grid.gameObject);

            long allocated = GCAssert.MeasureAllocatedBytes(
                () =>
                {
                    List<FastVector3Int> hull = points.BuildConcaveHullKnn(
                        grid,
                        nearestNeighbors: 8
                    );
                    Assert.IsNotNull(hull);
                    Assert.GreaterOrEqual(hull.Count, 3);
                },
                warmupIterations: 3,
                measuredIterations: 1
            );

            Assert.LessOrEqual(
                allocated,
                GridAllocationBudgetBytes,
                $"Grid concave hull should allocate no more than the hull list (measured {allocated} bytes)."
            );
        }

        [Test]
        public void BuildConcaveHullEdgeSplitRepairStatsStayBounded()
        {
            List<FastVector3Int> points = CreateConcaveGridSample(
                RepairSampleWidth,
                RepairSampleHeight
            );
            Grid grid = new GameObject("ConcaveHullRepairGrid").AddComponent<Grid>();
            Track(grid.gameObject);

            List<FastVector3Int> hull = points.BuildConcaveHull(
                grid,
                UnityExtensions.ConcaveHullOptions.ForEdgeSplit(
                    bucketSize: 64,
                    angleThreshold: 220f
                )
            );

            UnityExtensions.ConcaveHullRepairStats stats = UnityExtensions.ProfileConcaveHullRepair(
                hull,
                points,
                UnityExtensions.ConcaveHullStrategy.EdgeSplit,
                angleThreshold: 220f
            );

            Assert.Greater(
                stats.AxisCornerInsertions + stats.AxisPathInsertions,
                0,
                "Repair should insert axis-aligned corners for the carved cavity."
            );
            Assert.AreEqual(
                stats.FinalHullCount,
                hull.Count,
                "Profiled repair should mirror final hull size."
            );
            Assert.LessOrEqual(
                stats.FinalHullCount,
                stats.OriginalPointsCount,
                "Repair must not exceed the source point budget."
            );
            Assert.AreEqual(0, stats.DuplicateRemovals, "Repair should deduplicate as it goes.");
            Assert.Greater(stats.MaxFrontierSize, 0, "BFS frontier should process nodes.");
            Assert.Greater(stats.AxisNeighborVisits, 0, "BFS should traverse neighbors.");

            Debug.Log(
                $"[ConcaveHullPerformance] repair stats — Start:{stats.StartHullCount}, Final:{stats.FinalHullCount}, AxisCornerInsertions:{stats.AxisCornerInsertions}, AxisPathInsertions:{stats.AxisPathInsertions}, AxisNeighborVisits:{stats.AxisNeighborVisits}"
            );
        }

        [Test]
        public void BuildConcaveHullKnnGridAllocationsStayBoundedForLargePointCloud()
        {
            List<FastVector3Int> points = CreateGridPointCloud(LargeWidth, LargeHeight);
            Grid grid = new GameObject("ConcaveHullGridLarge").AddComponent<Grid>();
            Track(grid.gameObject);

            long allocated = GCAssert.MeasureAllocatedBytes(
                () =>
                {
                    List<FastVector3Int> hull = points.BuildConcaveHullKnn(
                        grid,
                        nearestNeighbors: 8
                    );
                    Assert.IsNotNull(hull);
                    Assert.GreaterOrEqual(hull.Count, 3);
                },
                warmupIterations: 2,
                measuredIterations: 1
            );

            Assert.LessOrEqual(
                allocated,
                LargeGridAllocationBudgetBytes,
                $"Grid concave hull (>10k points) should stay within hull list allocations (measured {allocated} bytes)."
            );
        }

        private static List<Vector2> CreateVectorPointCloud(int width, int height)
        {
            List<Vector2> points = new(width * height);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    points.Add(new Vector2(x, y));
                }
            }

            return points;
        }

        private static List<FastVector3Int> CreateGridPointCloud(int width, int height)
        {
            List<FastVector3Int> points = new(width * height);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    points.Add(new FastVector3Int(x, y, 0));
                }
            }

            return points;
        }

        private static List<FastVector3Int> CreateConcaveGridSample(int width, int height)
        {
            List<FastVector3Int> points = new(width * height);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (x > width / 4 && x < width * 3 / 4 && y > height / 4 && y < height * 3 / 4)
                    {
                        continue;
                    }

                    points.Add(new FastVector3Int(x, y, 0));
                }
            }

            return points;
        }
    }
}
