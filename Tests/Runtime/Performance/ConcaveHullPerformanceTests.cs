namespace WallstopStudios.UnityHelpers.Tests.Runtime.Performance
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

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
    }
}
