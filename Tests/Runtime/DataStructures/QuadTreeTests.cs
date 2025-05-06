namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using Core.DataStructure;
    using UnityEngine;

    public sealed class QuadTreeTests : SpatialTreeTests<QuadTree<Vector2>>
    {
        protected override QuadTree<Vector2> CreateTree(IEnumerable<Vector2> points)
        {
            return new QuadTree<Vector2>(points, _ => _);
        }
    }
}
