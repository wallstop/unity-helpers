// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using UnityEngine;

    /// <summary>
    /// Strategy dispatcher for concave hull builders.
    /// </summary>
    public static partial class UnityExtensions
    {
        public static List<Vector2> BuildConcaveHull(
            this IReadOnlyCollection<Vector2> points,
            ConcaveHullOptions options
        )
        {
            options ??= new ConcaveHullOptions();
            switch (options.Strategy)
            {
                case ConcaveHullStrategy.Knn:
                    return BuildConcaveHull2(points, Math.Max(3, options.NearestNeighbors));
                case ConcaveHullStrategy.EdgeSplit:
                    return BuildConcaveHull3(
                        points,
                        Math.Max(1, options.BucketSize),
                        options.AngleThreshold
                    );
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(options.Strategy),
                        (int)options.Strategy,
                        typeof(ConcaveHullStrategy)
                    );
            }
        }
    }
}
