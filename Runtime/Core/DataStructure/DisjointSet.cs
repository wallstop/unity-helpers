namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// A disjoint-set (union-find) data structure with path compression and union by rank.
    /// Essential for determining connectivity in graphs, procedural generation (maze/terrain),
    /// and grouping/clustering algorithms. Near-constant time O(α(n)) operations where α is
    /// the inverse Ackermann function.
    /// Works with integer indices for maximum performance.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public sealed class DisjointSet
    {
        [SerializeField]
        [ProtoMember(1)]
        private int[] _parent = Array.Empty<int>();

        [SerializeField]
        [ProtoMember(2)]
        private int[] _rank = Array.Empty<int>();

        [SerializeField]
        [ProtoMember(3)]
        private int _setCount;

        /// <summary>
        /// Gets the number of elements in the disjoint set.
        /// </summary>
        public int Count => _parent.Length;

        /// <summary>
        /// Gets the number of distinct sets.
        /// </summary>
        public int SetCount => _setCount;

        private DisjointSet() { }

        /// <summary>
        /// Constructs a disjoint set with n elements, each in its own set.
        /// </summary>
        /// <param name="n">The number of elements.</param>
        public DisjointSet(int n)
        {
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "Count must be positive.");
            }

            _parent = new int[n];
            _rank = new int[n];
            _setCount = n;

            for (int i = 0; i < n; i++)
            {
                _parent[i] = i;
                _rank[i] = 0;
            }
        }

        /// <summary>
        /// Attempts to find the representative (root) of the set containing element x.
        /// Uses path compression for optimization.
        /// </summary>
        /// <param name="x">The element to find.</param>
        /// <param name="representative">The representative if found.</param>
        /// <returns>True if x is valid, false otherwise.</returns>
        public bool TryFind(int x, out int representative)
        {
            if (x < 0 || x >= _parent.Length)
            {
                representative = -1;
                return false;
            }

            // Path compression
            if (_parent[x] != x)
            {
                _parent[x] = TryFindInternal(x);
            }
            representative = _parent[x];
            return true;
        }

        private int TryFindInternal(int x)
        {
            if (_parent[x] != x)
            {
                _parent[x] = TryFindInternal(_parent[x]);
            }
            return _parent[x];
        }

        /// <summary>
        /// Attempts to union the sets containing elements x and y.
        /// Uses union by rank for optimization.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <returns>True if successfully unioned (sets were different), false if invalid indices or already in same set.</returns>
        public bool TryUnion(int x, int y)
        {
            if (!TryFind(x, out int rootX) || !TryFind(y, out int rootY))
            {
                return false;
            }

            if (rootX == rootY)
            {
                return false; // Already in same set
            }

            // Union by rank
            if (_rank[rootX] < _rank[rootY])
            {
                _parent[rootX] = rootY;
            }
            else if (_rank[rootX] > _rank[rootY])
            {
                _parent[rootY] = rootX;
            }
            else
            {
                _parent[rootY] = rootX;
                _rank[rootX]++;
            }

            _setCount--;
            return true;
        }

        /// <summary>
        /// Attempts to check if elements x and y are in the same set.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <param name="connected">True if connected, false otherwise.</param>
        /// <returns>True if both indices are valid, false otherwise.</returns>
        public bool TryIsConnected(int x, int y, out bool connected)
        {
            if (!TryFind(x, out int rootX) || !TryFind(y, out int rootY))
            {
                connected = false;
                return false;
            }
            connected = rootX == rootY;
            return true;
        }

        /// <summary>
        /// Attempts to get the size of the set containing element x.
        /// </summary>
        public bool TryGetSetSize(int x, out int size)
        {
            if (!TryFind(x, out int root))
            {
                size = 0;
                return false;
            }

            size = 0;
            for (int i = 0; i < _parent.Length; i++)
            {
                if (TryFind(i, out int currentRoot) && currentRoot == root)
                {
                    size++;
                }
            }
            return true;
        }

        /// <summary>
        /// Populates the provided list with all elements in the same set as element x.
        /// Clears the list before adding. Returns the same list for chaining.
        /// Returns null if x is invalid.
        /// </summary>
        public List<int> TryGetSet(int x, List<int> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            if (!TryFind(x, out int root))
            {
                return results;
            }

            for (int i = 0; i < _parent.Length; i++)
            {
                if (TryFind(i, out int currentRoot) && currentRoot == root)
                {
                    results.Add(i);
                }
            }
            return results;
        }

        /// <summary>
        /// Populates the provided list with all distinct sets.
        /// Clears the list before adding. Returns the same list for chaining.
        /// Uses pooled temporary dictionary to avoid allocations.
        /// </summary>
        public List<List<int>> TryGetAllSets(List<List<int>> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            using PooledResource<Stack<List<int>>> stackResource = Buffers<List<int>>.Stack.Get(
                out Stack<List<int>> stack
            );
            foreach (List<int> input in results)
            {
                if (input != null)
                {
                    input.Clear();
                    stack.Push(input);
                }
            }

            results.Clear();

            using PooledResource<Dictionary<int, List<int>>> dictResource = DictionaryBuffer<
                int,
                List<int>
            >.Dictionary.Get(out Dictionary<int, List<int>> setMap);

            for (int i = 0; i < _parent.Length; i++)
            {
                if (!TryFind(i, out int root))
                {
                    continue;
                }

                if (!setMap.TryGetValue(root, out List<int> set))
                {
                    if (!stack.TryPop(out set))
                    {
                        set = new List<int>();
                    }

                    setMap[root] = set;
                }
                set.Add(i);
            }

            results.AddRange(setMap.Values);

            return results;
        }

        /// <summary>
        /// Resets the disjoint set so each element is in its own set.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _parent.Length; i++)
            {
                _parent[i] = i;
                _rank[i] = 0;
            }
            _setCount = _parent.Length;
        }
    }

    /// <summary>
    /// A generic disjoint-set (union-find) data structure that maps elements of type T to indices.
    /// Provides the same performance as DisjointSet with support for any element type.
    /// Uses a dictionary to map elements to internal indices.
    /// </summary>
    [Serializable]
    public sealed class DisjointSet<T>
    {
        private readonly DisjointSet _disjointSet;
        private readonly Dictionary<T, int> _elementToIndex;
        private readonly List<T> _indexToElement;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Gets the number of elements in the disjoint set.
        /// </summary>
        public int Count => _disjointSet.Count;

        /// <summary>
        /// Gets the number of distinct sets.
        /// </summary>
        public int SetCount => _disjointSet.SetCount;

        /// <summary>
        /// Constructs a disjoint set from a collection of elements.
        /// </summary>
        public DisjointSet(IEnumerable<T> elements, IEqualityComparer<T> comparer = null)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            _comparer = comparer ?? EqualityComparer<T>.Default;
            _elementToIndex = new Dictionary<T, int>(_comparer);
            _indexToElement = new List<T>();

            foreach (T element in elements)
            {
                if (_elementToIndex.TryAdd(element, _indexToElement.Count))
                {
                    _indexToElement.Add(element);
                }
            }

            if (_indexToElement.Count == 0)
            {
                throw new ArgumentException(
                    "Collection must contain at least one element.",
                    nameof(elements)
                );
            }

            _disjointSet = new DisjointSet(_indexToElement.Count);
        }

        /// <summary>
        /// Attempts to find the representative element of the set containing x.
        /// </summary>
        public bool TryFind(T x, out T representative)
        {
            if (!_elementToIndex.TryGetValue(x, out int index))
            {
                representative = default;
                return false;
            }

            if (!_disjointSet.TryFind(index, out int rootIndex))
            {
                representative = default;
                return false;
            }
            representative = _indexToElement[rootIndex];
            return true;
        }

        /// <summary>
        /// Attempts to union the sets containing elements x and y.
        /// </summary>
        /// <returns>True if the sets were unioned, false if invalid elements or already in same set.</returns>
        public bool TryUnion(T x, T y)
        {
            if (!_elementToIndex.TryGetValue(x, out int indexX))
            {
                return false;
            }
            if (!_elementToIndex.TryGetValue(y, out int indexY))
            {
                return false;
            }
            return _disjointSet.TryUnion(indexX, indexY);
        }

        /// <summary>
        /// Attempts to check if elements x and y are in the same set.
        /// </summary>
        public bool TryIsConnected(T x, T y, out bool connected)
        {
            if (!_elementToIndex.TryGetValue(x, out int indexX))
            {
                connected = false;
                return false;
            }

            if (!_elementToIndex.TryGetValue(y, out int indexY))
            {
                connected = false;
                return false;
            }

            return _disjointSet.TryIsConnected(indexX, indexY, out connected);
        }

        /// <summary>
        /// Attempts to get the size of the set containing element x.
        /// </summary>
        public bool TryGetSetSize(T x, out int size)
        {
            if (!_elementToIndex.TryGetValue(x, out int index))
            {
                size = 0;
                return false;
            }
            return _disjointSet.TryGetSetSize(index, out size);
        }

        /// <summary>
        /// Populates the provided list with all elements in the same set as x.
        /// Clears the list before adding. Returns the same list for chaining.
        /// Returns null if x is invalid.
        /// </summary>
        public List<T> TryGetSet(T x, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (!_elementToIndex.TryGetValue(x, out int index))
            {
                return results;
            }

            using PooledResource<List<int>> listResource = Buffers<int>.List.Get(
                out List<int> indices
            );
            foreach (int i in _disjointSet.TryGetSet(index, indices))
            {
                results.Add(_indexToElement[i]);
            }

            return results;
        }

        /// <summary>
        /// Populates the provided list with all distinct sets.
        /// Clears the list before adding. Returns the same list for chaining.
        /// </summary>
        public List<List<T>> TryGetAllSets(List<List<T>> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            using PooledResource<Stack<List<T>>> stackResource = Buffers<List<T>>.Stack.Get(
                out Stack<List<T>> stack
            );
            foreach (List<T> input in results)
            {
                if (input != null)
                {
                    input.Clear();
                    stack.Push(input);
                }
            }

            results.Clear();
            using PooledResource<List<List<int>>> listResource = Buffers<List<int>>.List.Get(
                out List<List<int>> indexSets
            );

            foreach (List<int> indexSet in _disjointSet.TryGetAllSets(indexSets))
            {
                if (!stack.TryPop(out List<T> elementSet))
                {
                    elementSet = new List<T>();
                }

                foreach (int i in indexSet)
                {
                    elementSet.Add(_indexToElement[i]);
                }
                results.Add(elementSet);
            }

            return results;
        }

        /// <summary>
        /// Resets the disjoint set so each element is in its own set.
        /// </summary>
        public void Reset()
        {
            _disjointSet.Reset();
        }
    }
}
