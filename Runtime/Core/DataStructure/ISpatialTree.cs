namespace UnityHelpers.Core.DataStructure
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface ISpatialTree<T>
    {
        Bounds Boundary { get; }
        IEnumerable<T> GetElementsInRange(Vector2 position, float range, float minimumRange = 0f);

        IEnumerable<T> GetElementsInBounds(Bounds bounds);

        void GetApproximateNearestNeighbors(Vector2 position, int count, List<T> nearestNeighbors);
    }
}
