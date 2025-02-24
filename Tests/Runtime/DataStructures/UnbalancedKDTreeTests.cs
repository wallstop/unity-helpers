namespace UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using Core.DataStructure;
    using UnityEngine;

    public sealed class UnbalancedKDTreeTests : SpatialTreeTests<KDTree<Vector2>>
    {
        protected override KDTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new KDTree<Vector2>(points, _ => _, balanced: false);
        }
    }
}
