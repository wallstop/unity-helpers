﻿namespace UnityHelpers.Tests.Performance
{
    using System.Collections.Generic;
    using UnityEngine;
    using Core.DataStructure;

    public sealed class UnbalancedKDTreeTests : SpatialTreePerformanceTest<KDTree<Vector2>>
    {
        protected override KDTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new KDTree<Vector2>(points, _ => _, balanced: false);
        }
    }
}
