namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class QuadTreeTests : SpatialTreeTests<QuadTree<Vector2>>
    {
        protected override QuadTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new QuadTree<Vector2>(points, _ => _);
        }
    }
}
