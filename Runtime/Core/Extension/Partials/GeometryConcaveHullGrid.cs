// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using DataStructure.Adapters;
    using UnityEngine;

    /// <summary>
    /// Grid-aware concave hull entry points (FastVector3Int + Grid contexts).
    /// </summary>
    public static partial class UnityExtensions
    {
        /// <summary>
        /// Unified concave hull entry point with explicit strategy handling for Grid contexts.
        /// </summary>
        public static List<FastVector3Int> BuildConcaveHull(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            ConcaveHullOptions options
        )
        {
            options ??= new ConcaveHullOptions();
            List<FastVector3Int> sourcePoints = ClonePositions(gridPositions);
            List<FastVector3Int> hull;
            switch (options.Strategy)
            {
                case ConcaveHullStrategy.Knn:
#pragma warning disable CS0618 // Type or member is obsolete
                    hull = BuildConcaveHull2(
                        gridPositions,
                        grid,
                        Math.Max(3, options.NearestNeighbors)
                    );
#pragma warning restore CS0618 // Type or member is obsolete
                    break;
                case ConcaveHullStrategy.EdgeSplit:
#pragma warning disable CS0618 // Type or member is obsolete
                    hull = BuildConcaveHull3(
                        gridPositions,
                        grid,
                        Math.Max(1, options.BucketSize),
                        options.AngleThreshold
                    );
#pragma warning restore CS0618 // Type or member is obsolete
                    break;
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(options.Strategy),
                        (int)options.Strategy,
                        typeof(ConcaveHullStrategy)
                    );
            }

#if ENABLE_CONCAVE_HULL_STATS
            ConcaveHullRepairStats repairStats = new(hull.Count, sourcePoints.Count);
            MaybeRepairConcaveCorners(
                hull,
                sourcePoints,
                options.Strategy,
                options.AngleThreshold,
                repairStats
            );
            TrackHullRepairStats(hull, repairStats);
#else
            MaybeRepairConcaveCorners(hull, sourcePoints, options.Strategy, options.AngleThreshold);
#endif
            return hull;
        }

        public static List<FastVector3Int> BuildConcaveHullKnn(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int nearestNeighbors = 3
        )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ConcaveHullOptions options = new()
            {
                Strategy = ConcaveHullStrategy.Knn,
                NearestNeighbors = Math.Max(3, nearestNeighbors),
            };
            return BuildConcaveHull(gridPositions, grid, options);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static List<FastVector3Int> BuildConcaveHullEdgeSplit(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            int clampedBucketSize = Math.Max(1, bucketSize);
            float effectiveAngleThreshold = clampedBucketSize <= 1 ? 0f : angleThreshold;
            ConcaveHullOptions options = new()
            {
                Strategy = ConcaveHullStrategy.EdgeSplit,
                BucketSize = clampedBucketSize,
                AngleThreshold = effectiveAngleThreshold,
            };
            return BuildConcaveHull(gridPositions, grid, options);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
