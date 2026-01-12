// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Disposable abstraction for 2D spatial hashes so DI/factory consumers can enforce lease cleanup.
    /// </summary>
    /// <typeparam name="T">Stored element type.</typeparam>
    public interface ISpatialHash2D<T> : IDisposable
    {
        float CellSize { get; }

        int CellCount { get; }

        void Insert(Vector2 position, T item);

        bool Remove(Vector2 position, T item);

        List<T> Query(
            Vector2 position,
            float radius,
            List<T> results,
            bool distinct = true,
            bool exactDistance = true
        );

        List<T> QueryRect(Rect rect, List<T> results, bool distinct = true);

        void Clear();
    }
}
