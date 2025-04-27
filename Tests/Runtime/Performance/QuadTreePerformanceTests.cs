﻿namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class QuadTreePerformanceTests : SpatialTreePerformanceTest<QuadTree<Vector2>>
    {
        protected override QuadTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new QuadTree<Vector2>(points, _ => _);
        }
    }
}
