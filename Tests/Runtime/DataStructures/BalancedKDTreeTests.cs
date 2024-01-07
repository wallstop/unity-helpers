namespace UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using UnityEngine;
    using Core.DataStructure;

    public sealed class BalancedKDTreeTests : SpatialTreeTests<KDTree<Vector2>>
    {
        protected override KDTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new KDTree<Vector2>(points, _ => _);
        }
    }
}
