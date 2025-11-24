namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Contract for 2D spatial trees (quad trees, k-d trees, etc.) that support range and nearest-neighbor queries.
    /// Enables algorithms to consume spatial containers without caring about the concrete backing structure.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// ISpatialTree2D<Enemy> tree = new QuadTree2D<Enemy>(worldBounds);
    /// List<Enemy> results = new List<Enemy>();
    /// tree.GetElementsInRange(playerPosition, 5f, results);
    /// ]]></code>
    /// </example>
    /// <remarks>
    /// <para><b>Result buffers:</b> Each query clears the provided <see cref="List{T}"/> before appending new entries. Reuse the same list between calls to avoid repeated allocations.</para>
    /// </remarks>
    public interface ISpatialTree2D<T>
    {
        Bounds Boundary { get; }

        List<T> GetElementsInRange(
            Vector2 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0f
        );

        List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds);

        List<T> GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        );
    }
}
