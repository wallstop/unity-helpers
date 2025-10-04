namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// A 2D spatial hash for fast broad-phase collision detection and neighbor queries.
    /// Simpler and more efficient than QuadTree for uniformly distributed objects.
    /// Perfect for particle systems, entity proximity checks, and collision culling.
    /// </summary>
    public sealed class SpatialHash2D<T>
    {
        private readonly Dictionary<FastVector2Int, List<T>> _grid;
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
            _grid = new Dictionary<FastVector2Int, List<T>>();
        }

        /// <summary>
        /// Inserts an item at the specified position.
        /// </summary>
        public void Insert(Vector2 position, T item)
        {
            FastVector2Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<T> items))
            {
                items = new List<T>();
                _grid[cell] = items;
            }
            items.Add(item);
        }

        /// <summary>
        /// Removes an item from the specified position.
        /// Returns true if found and removed.
        /// </summary>
        public bool Remove(Vector2 position, T item)
        {
            FastVector2Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<T> items))
            {
                return false;
            }

            bool removed = items.Remove(item);
            if (items.Count == 0)
            {
                _grid.Remove(cell);
            }
            return removed;
        }

        /// <summary>
        /// Queries all items within the specified radius of the position.
        /// Clears the results list before adding. Returns the same list for chaining.
        /// </summary>
        public List<T> Query(Vector2 position, float radius, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            int cellRadius = Mathf.CeilToInt(radius / _cellSize);
            FastVector2Int centerCell = GetCell(position);
            float radiusSquared = radius * radius;

            using PooledResource<HashSet<T>> setResource = Buffers<T>.HashSet.Get(
                out HashSet<T> seen
            );

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    FastVector2Int cell = new FastVector2Int(centerCell.x + x, centerCell.y + y);
                    if (_grid.TryGetValue(cell, out List<T> items))
                    {
                        foreach (T item in items)
                        {
                            if (seen.Add(item))
                            {
                                results.Add(item);
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
        public List<T> QueryRect(Vector2 min, Vector2 max, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            FastVector2Int minCell = GetCell(min);
            FastVector2Int maxCell = GetCell(max);

            using PooledResource<HashSet<T>> setResource = Buffers<T>.HashSet.Get(
                out HashSet<T> seen
            );

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    FastVector2Int cell = new FastVector2Int(x, y);
                    if (_grid.TryGetValue(cell, out List<T> items))
                    {
                        foreach (T item in items)
                        {
                            if (seen.Add(item))
                            {
                                results.Add(item);
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

        private FastVector2Int GetCell(Vector2 position)
        {
            return new FastVector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize)
            );
        }
    }

    /// <summary>
    /// A 3D spatial hash for fast broad-phase collision detection and neighbor queries.
    /// Simpler and more efficient than Octree for uniformly distributed objects.
    /// </summary>
    public sealed class SpatialHash3D<T>
    {
        private readonly Dictionary<FastVector3Int, List<T>> _grid;
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
        /// Constructs a 3D spatial hash with the specified cell size.
        /// </summary>
        public SpatialHash3D(float cellSize, IEqualityComparer<T> comparer = null)
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
            _grid = new Dictionary<FastVector3Int, List<T>>();
        }

        /// <summary>
        /// Inserts an item at the specified position.
        /// </summary>
        public void Insert(Vector3 position, T item)
        {
            FastVector3Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<T> items))
            {
                items = new List<T>();
                _grid[cell] = items;
            }
            items.Add(item);
        }

        /// <summary>
        /// Removes an item from the specified position.
        /// Returns true if found and removed.
        /// </summary>
        public bool Remove(Vector3 position, T item)
        {
            FastVector3Int cell = GetCell(position);
            if (!_grid.TryGetValue(cell, out List<T> items))
            {
                return false;
            }

            bool removed = items.Remove(item);
            if (items.Count == 0)
            {
                _grid.Remove(cell);
            }
            return removed;
        }

        /// <summary>
        /// Queries all items within the specified radius of the position.
        /// Clears the results list before adding. Returns the same list for chaining.
        /// </summary>
        public List<T> Query(Vector3 position, float radius, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            int cellRadius = Mathf.CeilToInt(radius / _cellSize);
            FastVector3Int centerCell = GetCell(position);

            using PooledResource<HashSet<T>> setResource = Buffers<T>.HashSet.Get(
                out HashSet<T> seen
            );

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        FastVector3Int cell = new FastVector3Int(
                            centerCell.x + x,
                            centerCell.y + y,
                            centerCell.z + z
                        );
                        if (_grid.TryGetValue(cell, out List<T> items))
                        {
                            foreach (T item in items)
                            {
                                if (seen.Add(item))
                                {
                                    results.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Queries all items within the specified box bounds.
        /// Clears the results list before adding. Returns the same list for chaining.
        /// </summary>
        public List<T> QueryBox(Vector3 min, Vector3 max, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            FastVector3Int minCell = GetCell(min);
            FastVector3Int maxCell = GetCell(max);

            using PooledResource<HashSet<T>> setResource = Buffers<T>.HashSet.Get(
                out HashSet<T> seen
            );

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        FastVector3Int cell = new FastVector3Int(x, y, z);
                        if (_grid.TryGetValue(cell, out List<T> items))
                        {
                            foreach (T item in items)
                            {
                                if (seen.Add(item))
                                {
                                    results.Add(item);
                                }
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

        private FastVector3Int GetCell(Vector3 position)
        {
            return new FastVector3Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize)
            );
        }
    }
}
