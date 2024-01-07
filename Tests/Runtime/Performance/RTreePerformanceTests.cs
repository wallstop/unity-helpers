namespace UnityHelpers.Tests.Performance
{
    using System.Collections.Generic;
    using UnityEngine;
    using Core.DataStructure;

    public sealed class RTreePerformanceTests : SpatialTreePerformanceTest<RTree<Vector2>>
    {
        protected override RTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new RTree<Vector2>(points, _ => _);
        }
    }
}
