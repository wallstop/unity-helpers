namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface ISpatialTree<T>
    {
        Bounds Boundary { get; }

        List<T> GetElementsInRange(
            Vector2 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0f
        );

        List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds);

        void GetApproximateNearestNeighbors(Vector2 position, int count, List<T> nearestNeighbors);
    }
}
