﻿namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// A 2D spatial hash for fast broad-phase collision detection and neighbor queries.
    /// Simpler and more efficient than QuadTree for uniformly distributed objects.
    /// Perfect for particle systems, entity proximity checks, and collision culling.
    /// </summary>
    [Serializable]
    public sealed class SpatialHash2D<T> : IDisposable
    {
        private readonly struct Entry
        {
            public readonly Vector2 position;
            public readonly T item;

            public Entry(Vector2 position, T item)
            {
                this.position = position;
                this.item = item;
            }
        }

        private readonly Dictionary<FastVector2Int, List<Entry>> _grid;
        private readonly float _cellSize;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Gets the cell size of the spatial hash.
        /// </summary>
        public float CellSize => _cellSize;

        /// <summary>
        /// Gets the total number of occupied cells.
        /// </summary>
        public int CellCount => _grid.Count;

        /// <summary>
        /// Constructs a 2D spatial hash with the specified cell size.
        /// </summary>
        public SpatialHash2D(float cellSize, IEqualityComparer<T> comparer = null)
        {
            if (cellSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize),
                    "Cell size must be positive."
                );
            }

            _cellSize = cellSize;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _grid = new Dictionary<FastVector2Int, List<Entry>>();
        }

        /// <summary>
        /// Inserts an item at the specified position.
        /// </summary>
        public void Insert(Vector2 position, T item)
        {
            FastVector2Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<Entry> entries))
            {
                entries = new List<Entry>();
                _grid[cell] = entries;
            }
            entries.Add(new Entry(position, item));
        }

        /// <summary>
        /// Removes an item from the specified position.
        /// Returns true if found and removed.
        /// </summary>
        public bool Remove(Vector2 position, T item)
        {
            FastVector2Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<Entry> entries))
            {
                return false;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];
                if (!entry.position.Equals(position) || !_comparer.Equals(entry.item, item))
                {
                    continue;
                }

                entries.RemoveAt(i);
                if (entries.Count == 0)
                {
                    _grid.Remove(cell);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Queries all items within the specified radius of the position.
        /// Clears the results list before adding. Returns the same list for chaining.
        /// </summary>
        /// <param name="position">The center position of the query.</param>
        /// <param name="radius">The radius to search within.</param>
        /// <param name="results">The list to store results in.</param>
        /// <param name="distinct">Whether to return distinct items only.</param>
        /// <param name="exactDistance">If true, performs exact distance checking. If false, returns all items in cells that intersect the query radius (faster but may include extra items).</param>
        public List<T> Query(
            Vector2 position,
            float radius,
            List<T> results,
            bool distinct = true,
            bool exactDistance = true
        )
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            int cellRadius = Mathf.CeilToInt(radius / _cellSize);
            FastVector2Int centerCell = GetCell(position);
            float radiusSquared = radius * radius;

            if (distinct)
            {
                using PooledResource<HashSet<T>> setResource = SetBuffers<T>
                    .GetHashSetPool(_comparer)
                    .Get(out HashSet<T> seen);

                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    for (int y = -cellRadius; y <= cellRadius; y++)
                    {
                        FastVector2Int cell = new(centerCell.x + x, centerCell.y + y);
                        if (!_grid.TryGetValue(cell, out List<Entry> entries))
                        {
                            continue;
                        }

                        foreach (Entry entry in entries)
                        {
                            if (!exactDistance)
                            {
                                if (seen.Add(entry.item))
                                {
                                    results.Add(entry.item);
                                }
                            }
                            else
                            {
                                float distSq = (entry.position - position).sqrMagnitude;
                                if (distSq <= radiusSquared && seen.Add(entry.item))
                                {
                                    results.Add(entry.item);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    for (int y = -cellRadius; y <= cellRadius; y++)
                    {
                        FastVector2Int cell = new(centerCell.x + x, centerCell.y + y);
                        if (!_grid.TryGetValue(cell, out List<Entry> entries))
                        {
                            continue;
                        }

                        foreach (Entry entry in entries)
                        {
                            if (!exactDistance)
                            {
                                results.Add(entry.item);
                            }
                            else
                            {
                                float distSq = (entry.position - position).sqrMagnitude;
                                if (distSq <= radiusSquared)
                                {
                                    results.Add(entry.item);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Queries all items within the specified rectangular bounds.
        /// Clears the results list before adding. Returns the same list for chaining.
        /// </summary>
        public List<T> QueryRect(Rect rect, List<T> results, bool distinct = true)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            Vector2 min = rect.min;
            Vector2 max = rect.max;
            FastVector2Int minCell = GetCell(min);
            FastVector2Int maxCell = GetCell(max);

            if (distinct)
            {
                using PooledResource<HashSet<T>> setResource = SetBuffers<T>
                    .GetHashSetPool(_comparer)
                    .Get(out HashSet<T> seen);

                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        FastVector2Int cell = new(x, y);
                        if (!_grid.TryGetValue(cell, out List<Entry> entries))
                        {
                            continue;
                        }

                        foreach (Entry entry in entries)
                        {
                            Vector2 pos = entry.position;
                            if (
                                pos.x >= min.x
                                && pos.x <= max.x
                                && pos.y >= min.y
                                && pos.y <= max.y
                            )
                            {
                                if (seen.Add(entry.item))
                                {
                                    results.Add(entry.item);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        FastVector2Int cell = new(x, y);
                        if (!_grid.TryGetValue(cell, out List<Entry> entries))
                        {
                            continue;
                        }

                        foreach (Entry entry in entries)
                        {
                            Vector2 pos = entry.position;
                            if (
                                pos.x >= min.x
                                && pos.x <= max.x
                                && pos.y >= min.y
                                && pos.y <= max.y
                            )
                            {
                                results.Add(entry.item);
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Clears all items from the spatial hash.
        /// </summary>
        public void Clear()
        {
            _grid.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FastVector2Int GetCell(Vector2 position)
        {
            return new FastVector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize)
            );
        }

        public void Dispose()
        {
            SetBuffers<T>.DestroyHashSetPool(_comparer);
        }
    }
}
