namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System.Collections.Generic;
    using Core.DataStructure;
    using UnityEngine;

    public sealed class KDTreePerformanceTests : SpatialTreePerformanceTest<KDTree<Vector2>>
    {
        protected override KDTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new KDTree<Vector2>(points, _ => _);
        }
    }
}
