namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for 3D spatial tree data structures.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the tree.</typeparam>
    /// <remarks>
    /// <para><b>⚠️ EXPERIMENTAL:</b> 3D spatial trees are currently experimental and under active development.</para>
    /// <para>APIs may change, and performance characteristics may vary. Use with caution in production environments.</para>
    /// </remarks>
    public interface ISpatialTree3D<T>
    {
        Bounds Boundary { get; }

        List<T> GetElementsInRange(
            Vector3 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0f
        );

        List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds);

        List<T> GetApproximateNearestNeighbors(
            Vector3 position,
            int count,
            List<T> nearestNeighbors
        );
    }
}
