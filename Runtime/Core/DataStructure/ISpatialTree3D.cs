// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Contract for 3D spatial trees (octrees, kd-trees, etc.) that expose range, bounds, and nearest-neighbor queries.
    /// Lets gameplay systems pick the most suitable spatial index without changing their query logic.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// ISpatialTree3D<Collider> tree = new OctTree3D<Collider>(worldBounds);
    /// List<Collider> results = new List<Collider>();
    /// tree.GetElementsInRange(playerPosition, 10f, results);
    /// ]]></code>
    /// </example>
    /// <typeparam name="T">The type of elements stored in the tree.</typeparam>
    /// <remarks>
    /// <para><b>⚠️ EXPERIMENTAL:</b> 3D spatial trees are currently experimental and under active development.</para>
    /// <para>APIs may change, and performance characteristics may vary. Use with caution in production environments.</para>
    /// <para><b>Result buffers:</b> Every query method clears the supplied <see cref="List{T}"/> before writing results. Pass a reusable buffer when you want to minimize allocations.</para>
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
