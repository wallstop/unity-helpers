// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Disposable abstraction for 3D spatial hashes so pooled resources are always released.
    /// </summary>
    /// <typeparam name="T">Stored element type.</typeparam>
    public interface ISpatialHash3D<T> : IDisposable
    {
        float CellSize { get; }

        int CellCount { get; }

        void Insert(Vector3 position, T item);

        bool Remove(Vector3 position, T item);

        List<T> Query(
            Vector3 position,
            float radius,
            List<T> results,
            bool distinct = true,
            bool exactDistance = true
        );

        List<T> QueryBox(Bounds bounds, List<T> results, bool distinct = true);

        void Clear();
    }
}
