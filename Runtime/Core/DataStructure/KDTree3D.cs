namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Immutable 3D k-d tree for efficient nearest neighbor, range, and bounds queries in 3D space.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// KdTree3D<Vector3>.Entry[] entries = points.Select(p => new KdTree3D<Vector3>.Entry(p, p)).ToArray();
    /// KdTree3D<Vector3> tree = KdTree3D<Vector3>.Build(entries);
    /// List<Vector3> neighbors = new List<Vector3>();
    /// tree.GetElementsInRange(queryPosition, 4f, neighbors);
    /// ]]></code>
    /// </example>
    /// <typeparam name="T">Element type contained in the tree.</typeparam>
    /// <remarks>
    /// <para>Pros: Very fast nearest neighbor performance; good for static or batched updates.</para>
    /// <para>Cons: Immutable structure by design; rebuild when positions change frequently.</para>
    /// <para>Semantics: Due to algorithmic choices (axis-aligned splitting, half-open containment checks,
    /// minimum node-size enforcement, and tie-handling on split planes), KdTree3D (balanced and unbalanced)
    /// may return different edge-case results compared to OctTree3D for identical inputs/queries—especially for
    /// points lying exactly on query boundaries or split planes. See Docs/SPATIAL_TREE_SEMANTICS.md for details.</para>
    /// </remarks>
    [Serializable]
    public sealed class KdTree3D<T> : ISpatialTree3D<T>
    {
        [Serializable]
        public readonly struct Entry
        {
            public readonly T value;
            public readonly Vector3 position;

            public Entry(T value, Vector3 position)
            {
                this.value = value;
                this.position = position;
            }
        }

        private readonly struct Neighbor
        {
            public readonly T value;
            public readonly float sqrDistance;

            public Neighbor(T value, float sqrDistance)
            {
                this.value = value;
                this.sqrDistance = sqrDistance;
            }
        }

        [Serializable]
        public sealed class KdTreeNode
        {
            public readonly Bounds boundary;
            public readonly KdTreeNode left;
            public readonly KdTreeNode right;
            internal readonly int _startIndex;
            internal readonly int _count;
            public readonly bool isTerminal;

            private KdTreeNode(
                Bounds boundary,
                KdTreeNode left,
                KdTreeNode right,
                int startIndex,
                int count,
                bool isTerminal
            )
            {
                this.boundary = boundary;
                this.left = left;
                this.right = right;
                _startIndex = startIndex;
                _count = count;
                this.isTerminal = isTerminal;
            }

            internal static KdTreeNode CreateLeaf(Bounds boundary, int startIndex, int count)
            {
                return new KdTreeNode(boundary, null, null, startIndex, count, true);
            }

            internal static KdTreeNode CreateInternal(
                Bounds boundary,
                KdTreeNode left,
                KdTreeNode right,
                int startIndex,
                int count
            )
            {
                return new KdTreeNode(boundary, left, right, startIndex, count, false);
            }
        }

        private const float MinimumNodeSize = 0.001f;
        private const int SmallPartitionThreshold = 32;

        /// <summary>
        /// Default bucket size for leaves before stopping recursion.
        /// </summary>
        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;

        /// <summary>
        /// Gets the overall bounding box of the tree.
        /// </summary>
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Entry[] _entries;
        private readonly int[] _indices;
        private readonly KdTreeNode _head;
        private readonly bool _balanced;
        private readonly int _bucketSize;

        /// <summary>
        /// Builds a 3D k-d tree from elements using a transformer to extract 3D positions.
        /// </summary>
        /// <param name="points">Source elements.</param>
        /// <param name="elementTransformer">Maps element to its 3D position.</param>
        /// <param name="bucketSize">Max elements per leaf. Minimum 1.</param>
        /// <param name="balanced">If true, builds a balanced tree by median selection; otherwise uses a quick split strategy.</param>
        /// <exception cref="ArgumentNullException">Thrown when points or elementTransformer are null.</exception>
        public KdTree3D(
            IEnumerable<T> points,
            Func<T, Vector3> elementTransformer,
            int bucketSize = DefaultBucketSize,
            bool balanced = true
        )
        {
            if (elementTransformer is null)
            {
                throw new ArgumentNullException(nameof(elementTransformer));
            }

            elements =
                points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));
            int elementCount = elements.Length;
            _entries = elementCount == 0 ? Array.Empty<Entry>() : new Entry[elementCount];
            _indices = elementCount == 0 ? Array.Empty<int>() : new int[elementCount];
            _balanced = balanced;
            _bucketSize = Math.Max(1, bucketSize);

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;

            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];
                Vector3 position = elementTransformer(element);
                _entries[i] = new Entry(element, position);

                if (position.x < minX)
                {
                    minX = position.x;
                }
                if (position.y < minY)
                {
                    minY = position.y;
                }
                if (position.z < minZ)
                {
                    minZ = position.z;
                }
                if (position.x > maxX)
                {
                    maxX = position.x;
                }
                if (position.y > maxY)
                {
                    maxY = position.y;
                }
                if (position.z > maxZ)
                {
                    maxZ = position.z;
                }

                _indices[i] = i;
            }

            Bounds bounds = CreateBounds(minX, maxX, minY, maxY, minZ, maxZ);

            if (elementCount == 0)
            {
                _bounds = bounds;
                _head = KdTreeNode.CreateLeaf(_bounds, 0, 0);
                return;
            }

            KdTreeNode root;
            if (_balanced)
            {
                root = BuildBalanced(0, elementCount, depth: 0);
            }
            else
            {
                int[] scratch = ArrayPool<int>.Shared.Rent(elementCount);
                try
                {
                    root = BuildUnbalanced(0, elementCount, axis: 0, scratch);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(scratch, clearArray: true);
                }
            }

            _head = root;
            _bounds = root.boundary;
        }

        private KdTreeNode BuildBalanced(int startIndex, int count, int depth)
        {
            if (count <= _bucketSize)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KdTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            int axis = depth % 3;

            Span<int> span = _indices.AsSpan(startIndex, count);
            int leftCount = count / 2;
            if (leftCount == 0)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KdTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            SelectKth(span, leftCount, axis);
            int rightCount = count - leftCount;
            if (rightCount == 0)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KdTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            KdTreeNode left = BuildBalanced(startIndex, leftCount, depth + 1);
            KdTreeNode right = BuildBalanced(startIndex + leftCount, rightCount, depth + 1);
            Bounds boundary = CombineChildBounds(left.boundary, right.boundary);
            return KdTreeNode.CreateInternal(boundary, left, right, startIndex, count);
        }

        private KdTreeNode BuildUnbalanced(int startIndex, int count, int axis, int[] scratch)
        {
            Span<int> source = _indices.AsSpan(startIndex, count);
            Span<int> temp = scratch.AsSpan(0, count);
            Entry[] entries = _entries;

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;

            for (int i = 0; i < count; ++i)
            {
                Vector3 position = entries[source[i]].position;
                if (position.x < minX)
                {
                    minX = position.x;
                }
                if (position.y < minY)
                {
                    minY = position.y;
                }
                if (position.z < minZ)
                {
                    minZ = position.z;
                }
                if (position.x > maxX)
                {
                    maxX = position.x;
                }
                if (position.y > maxY)
                {
                    maxY = position.y;
                }
                if (position.z > maxZ)
                {
                    maxZ = position.z;
                }
            }

            Bounds nodeBounds = CreateBounds(minX, maxX, minY, maxY, minZ, maxZ);

            if (count <= _bucketSize)
            {
                return KdTreeNode.CreateLeaf(nodeBounds, startIndex, count);
            }

            float cutoff = GetAxisValue(nodeBounds.center, axis);

            int leftWrite = 0;
            int rightWrite = count - 1;
            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector3 position = entries[entryIndex].position;
                float value = GetAxisValue(position, axis);
                if (value <= cutoff)
                {
                    temp[leftWrite++] = entryIndex;
                }
                else
                {
                    temp[rightWrite--] = entryIndex;
                }
            }

            int leftCount = leftWrite;
            int rightCount = count - leftCount;

            if (leftCount == 0 || rightCount == 0)
            {
                return KdTreeNode.CreateLeaf(nodeBounds, startIndex, count);
            }

            temp.CopyTo(source);

            int nextAxis = (axis + 1) % 3;
            KdTreeNode left = BuildUnbalanced(startIndex, leftCount, nextAxis, scratch);
            KdTreeNode right = BuildUnbalanced(
                startIndex + leftCount,
                rightCount,
                nextAxis,
                scratch
            );
            Bounds boundary = CombineChildBounds(left.boundary, right.boundary);
            return KdTreeNode.CreateInternal(boundary, left, right, startIndex, count);
        }

        private Bounds CalculateLeafBounds(int startIndex, int count)
        {
            if (count <= 0)
            {
                return new Bounds();
            }

            Entry[] entries = _entries;
            int[] indices = _indices;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;

            int end = startIndex + count;
            for (int i = startIndex; i < end; ++i)
            {
                Vector3 position = entries[indices[i]].position;
                if (position.x < minX)
                {
                    minX = position.x;
                }
                if (position.y < minY)
                {
                    minY = position.y;
                }
                if (position.z < minZ)
                {
                    minZ = position.z;
                }
                if (position.x > maxX)
                {
                    maxX = position.x;
                }
                if (position.y > maxY)
                {
                    maxY = position.y;
                }
                if (position.z > maxZ)
                {
                    maxZ = position.z;
                }
            }

            return CreateBounds(minX, maxX, minY, maxY, minZ, maxZ);
        }

        private void SelectKth(Span<int> span, int k, int axis)
        {
            Entry[] entries = _entries;
            int left = 0;
            int right = span.Length - 1;

            while (left < right)
            {
                if (right - left <= SmallPartitionThreshold)
                {
                    InsertionSort(span.Slice(left, right - left + 1), axis, entries);
                    return;
                }

                int pivotIndex = SelectPivot(span, left, right, axis, entries);
                float pivot = GetAxis(entries[span[pivotIndex]], axis);

                int i = left;
                int j = right;

                while (i <= j)
                {
                    while (i <= j && GetAxis(entries[span[i]], axis) < pivot)
                    {
                        i++;
                    }

                    while (i <= j && GetAxis(entries[span[j]], axis) > pivot)
                    {
                        j--;
                    }

                    if (i <= j)
                    {
                        (span[i], span[j]) = (span[j], span[i]);
                        i++;
                        j--;
                    }
                }

                if (k <= j)
                {
                    right = j;
                    continue;
                }

                if (k >= i)
                {
                    left = i;
                    continue;
                }

                return;
            }
        }

        private static int SelectPivot(
            Span<int> span,
            int left,
            int right,
            int axis,
            Entry[] entries
        )
        {
            int mid = left + ((right - left) >> 1);

            float leftValue = GetAxis(entries[span[left]], axis);
            float midValue = GetAxis(entries[span[mid]], axis);
            float rightValue = GetAxis(entries[span[right]], axis);

            if (leftValue > midValue)
            {
                (span[left], span[mid]) = (span[mid], span[left]);
                (leftValue, midValue) = (midValue, leftValue);
            }

            if (midValue > rightValue)
            {
                (span[mid], span[right]) = (span[right], span[mid]);
                (midValue, rightValue) = (rightValue, midValue);

                if (leftValue > midValue)
                {
                    (span[left], span[mid]) = (span[mid], span[left]);
                    (leftValue, midValue) = (midValue, leftValue);
                }
            }

            return mid;
        }

        private static void InsertionSort(Span<int> span, int axis, Entry[] entries)
        {
            if (span.Length <= 1)
            {
                return;
            }

            for (int i = 1; i < span.Length; ++i)
            {
                int currentIndex = span[i];
                float currentValue = GetAxis(entries[currentIndex], axis);
                int j = i - 1;

                while (j >= 0 && GetAxis(entries[span[j]], axis) > currentValue)
                {
                    span[j + 1] = span[j];
                    j--;
                }

                span[j + 1] = currentIndex;
            }
        }

        private static float GetAxis(in Entry entry, int axis)
        {
            return axis switch
            {
                0 => entry.position.x,
                1 => entry.position.y,
                _ => entry.position.z,
            };
        }

        private static float GetAxisValue(Vector3 position, int axis)
        {
            return axis switch
            {
                0 => position.x,
                1 => position.y,
                _ => position.z,
            };
        }

        private static Bounds CombineChildBounds(Bounds left, Bounds right)
        {
            Bounds combined = left;
            combined.Encapsulate(right);
            EnsureMinimumBounds(ref combined);
            return combined;
        }

        private static Bounds CreateBounds(
            float minX,
            float maxX,
            float minY,
            float maxY,
            float minZ,
            float maxZ
        )
        {
            if (float.IsInfinity(minX) || float.IsInfinity(minY) || float.IsInfinity(minZ))
            {
                return new Bounds();
            }

            Vector3 min = new(minX, minY, minZ);
            Vector3 max = new(maxX, maxY, maxZ);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            Bounds bounds = new(center, size);
            EnsureMinimumBounds(ref bounds);
            return bounds;
        }

        private static void EnsureMinimumBounds(ref Bounds bounds)
        {
            Vector3 size = bounds.size;
            if (size.x < MinimumNodeSize)
            {
                size.x = MinimumNodeSize;
            }
            if (size.y < MinimumNodeSize)
            {
                size.y = MinimumNodeSize;
            }
            if (size.z < MinimumNodeSize)
            {
                size.z = MinimumNodeSize;
            }
            bounds.size = size;
        }

        public List<T> GetElementsInRange(
            Vector3 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0
        )
        {
            elementsInRange.Clear();
            // Allow zero range to return only exact matches (distance == 0)
            if (range < 0f || _head._count <= 0)
            {
                return elementsInRange;
            }

            Sphere querySphere = new(position, range);
            Bounds bounds = new(position, new Vector3(range * 2f, range * 2f, range * 2f));

            if (!bounds.Intersects(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<Stack<KdTreeNode>> stackResource = Buffers<KdTreeNode>.Stack.Get();
            Stack<KdTreeNode> nodesToVisit = stackResource.resource;
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;
            float rangeSquared = range * range;
            bool hasMinimumRange = 0f < minimumRange;
            float minimumRangeSquared = minimumRange * minimumRange;
            Sphere minimumSphere = hasMinimumRange ? new Sphere(position, minimumRange) : default;

            while (nodesToVisit.TryPop(out KdTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (!bounds.Intersects(currentNode.boundary))
                {
                    continue;
                }

                // Use Sphere.Overlaps to check if the sphere fully contains the node's boundary
                BoundingBox3D nodeBoundary = BoundingBox3D.FromClosedBounds(currentNode.boundary);
                bool nodeFullyContained = querySphere.Overlaps(nodeBoundary);

                if (currentNode.isTerminal || nodeFullyContained)
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;

                    // If the node is fully contained, we can skip distance checks for points
                    if (nodeFullyContained)
                    {
                        if (!hasMinimumRange)
                        {
                            // Fast path: all points in this node are within range
                            for (int i = start; i < end; ++i)
                            {
                                elementsInRange.Add(entries[indices[i]].value);
                            }
                        }
                        else
                        {
                            // Node is fully in outer sphere, but need to check minimum range
                            // Check if node is fully outside minimum sphere
                            bool nodeFullyOutsideMinimum = !minimumSphere.Intersects(nodeBoundary);
                            if (nodeFullyOutsideMinimum)
                            {
                                // Fast path: all points are in the annulus
                                for (int i = start; i < end; ++i)
                                {
                                    elementsInRange.Add(entries[indices[i]].value);
                                }
                            }
                            else
                            {
                                // Need to check each point against minimum range
                                for (int i = start; i < end; ++i)
                                {
                                    Entry entry = entries[indices[i]];
                                    float squareDistance = (entry.position - position).sqrMagnitude;
                                    if (squareDistance > minimumRangeSquared)
                                    {
                                        elementsInRange.Add(entry.value);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Terminal node but not fully contained: check each point
                        for (int i = start; i < end; ++i)
                        {
                            Entry entry = entries[indices[i]];
                            float squareDistance = (entry.position - position).sqrMagnitude;
                            if (squareDistance > rangeSquared)
                            {
                                continue;
                            }

                            if (hasMinimumRange && squareDistance <= minimumRangeSquared)
                            {
                                continue;
                            }

                            elementsInRange.Add(entry.value);
                        }
                    }

                    continue;
                }

                KdTreeNode left = currentNode.left;
                if (left is not null && left._count > 0 && bounds.Intersects(left.boundary))
                {
                    nodesToVisit.Push(left);
                }

                KdTreeNode right = currentNode.right;
                if (right is not null && right._count > 0 && bounds.Intersects(right.boundary))
                {
                    nodesToVisit.Push(right);
                }
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            elementsInBounds.Clear();
            if (_head._count <= 0)
            {
                return elementsInBounds;
            }

            // Use closed Unity Bounds intersection for traversal to avoid pruning
            // legitimate edge cases; final per-point checks use closed semantics.
            if (!bounds.Intersects(_bounds))
            {
                return elementsInBounds;
            }

            // Build inclusive half-open query box for robust per-point checks
            BoundingBox3D queryBox = BoundingBox3D.FromClosedBoundsInclusiveMax(bounds);

            using PooledResource<Stack<KdTreeNode>> stackResource = Buffers<KdTreeNode>.Stack.Get(
                out Stack<KdTreeNode> nodesToVisit
            );
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;

            while (nodesToVisit.TryPop(out KdTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (currentNode.isTerminal)
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;
                    for (int i = start; i < end; ++i)
                    {
                        Entry entry = entries[indices[i]];
                        // Use inclusive half-open check for robust closed semantics
                        if (queryBox.Contains(entry.position))
                        {
                            elementsInBounds.Add(entry.value);
                        }
                    }

                    continue;
                }

                // Once we've reached an internal node that intersects the query,
                // visit both non-empty children and rely on per-point checks.
                KdTreeNode left = currentNode.left;
                if (left is not null && left._count > 0)
                {
                    nodesToVisit.Push(left);
                }

                KdTreeNode right = currentNode.right;
                if (right is not null && right._count > 0)
                {
                    nodesToVisit.Push(right);
                }
            }

            return elementsInBounds;
        }

        public List<T> GetApproximateNearestNeighbors(
            Vector3 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            nearestNeighbors.Clear();

            if (count <= 0 || _head._count == 0)
            {
                return nearestNeighbors;
            }

            using PooledResource<Stack<KdTreeNode>> nodeBufferResource =
                Buffers<KdTreeNode>.Stack.Get(out Stack<KdTreeNode> nodeBuffer);
            nodeBuffer.Push(_head);
            using PooledResource<HashSet<T>> nearestNeighborBufferResource = Buffers<T>.HashSet.Get(
                out HashSet<T> nearestNeighborBuffer
            );
            using PooledResource<List<Neighbor>> neighborCandidatesResource =
                Buffers<Neighbor>.List.Get(out List<Neighbor> neighborCandidates);

            Entry[] entries = _entries;
            int[] indices = _indices;

            KdTreeNode current = _head;

            while (!current.isTerminal)
            {
                KdTreeNode left = current.left;
                KdTreeNode right = current.right;
                if (left is null || left._count == 0)
                {
                    if (right is null || right._count == 0)
                    {
                        break;
                    }

                    nodeBuffer.Push(right);
                    current = right;
                    if (right._count <= count)
                    {
                        break;
                    }

                    continue;
                }

                if (right is null || right._count == 0)
                {
                    nodeBuffer.Push(left);
                    current = left;
                    if (left._count <= count)
                    {
                        break;
                    }

                    continue;
                }

                float leftDistance = (left.boundary.center - position).sqrMagnitude;
                float rightDistance = (right.boundary.center - position).sqrMagnitude;
                if (leftDistance < rightDistance)
                {
                    if (right._count > 0)
                    {
                        nodeBuffer.Push(right);
                    }

                    nodeBuffer.Push(left);
                    current = left;
                    if (left._count <= count)
                    {
                        break;
                    }
                }
                else
                {
                    if (left._count > 0)
                    {
                        nodeBuffer.Push(left);
                    }

                    nodeBuffer.Push(right);
                    current = right;
                    if (right._count <= count)
                    {
                        break;
                    }
                }
            }

            while (
                nearestNeighborBuffer.Count < count && nodeBuffer.TryPop(out KdTreeNode selected)
            )
            {
                if (selected is null || selected._count <= 0)
                {
                    continue;
                }

                int startIndex = selected._startIndex;
                int endIndex = startIndex + selected._count;
                for (int i = startIndex; i < endIndex; ++i)
                {
                    Entry entry = entries[indices[i]];
                    if (!nearestNeighborBuffer.Add(entry.value))
                    {
                        continue;
                    }

                    float sqrDistance = (entry.position - position).sqrMagnitude;
                    neighborCandidates.Add(new Neighbor(entry.value, sqrDistance));
                }
            }

            if (neighborCandidates.Count > 1)
            {
                neighborCandidates.Sort(NeighborComparer.Instance);
            }

            nearestNeighbors.Clear();
            for (int i = 0; i < neighborCandidates.Count && i < count; ++i)
            {
                nearestNeighbors.Add(neighborCandidates[i].value);
            }

            return nearestNeighbors;
        }

        private sealed class NeighborComparer : IComparer<Neighbor>
        {
            internal static readonly NeighborComparer Instance = new();

            public int Compare(Neighbor x, Neighbor y)
            {
                return x.sqrDistance.CompareTo(y.sqrDistance);
            }
        }
    }
}
