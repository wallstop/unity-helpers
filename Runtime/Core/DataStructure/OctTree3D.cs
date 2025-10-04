namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class OctTree3D<T> : ISpatialTree3D<T>
    {
        private const int NumChildren = 8;

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

        [Serializable]
        public sealed class OctTreeNode
        {
            public readonly Bounds boundary;
            internal readonly OctTreeNode[] _children;
            internal readonly int _startIndex;
            internal readonly int _count;
            public readonly bool isTerminal;

            private OctTreeNode(
                Bounds boundary,
                int startIndex,
                int count,
                bool isTerminal,
                OctTreeNode[] children
            )
            {
                this.boundary = boundary;
                _startIndex = startIndex;
                _count = count;
                this.isTerminal = isTerminal;
                _children = children ?? Array.Empty<OctTreeNode>();
            }

            internal static OctTreeNode CreateLeaf(Bounds boundary, int startIndex, int count)
            {
                return new OctTreeNode(
                    boundary,
                    startIndex,
                    count,
                    true,
                    Array.Empty<OctTreeNode>()
                );
            }

            internal static OctTreeNode CreateInternal(
                Bounds boundary,
                OctTreeNode[] children,
                int startIndex,
                int count
            )
            {
                return new OctTreeNode(boundary, startIndex, count, false, children);
            }
        }

        private readonly struct NodeDistance
        {
            internal readonly OctTreeNode _node;
            internal readonly float _distanceSquared;

            internal NodeDistance(OctTreeNode node, float distanceSquared)
            {
                _node = node;
                _distanceSquared = distanceSquared;
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Entry[] _entries;
        private readonly int[] _indices;
        private readonly OctTreeNode _head;

        public OctTree3D(
            IEnumerable<T> points,
            Func<T, Vector3> elementTransformer,
            Bounds? boundary = null,
            int bucketSize = DefaultBucketSize
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

            Bounds bounds = boundary ?? default;
            bool anyPoints = boundary.HasValue;

            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];
                Vector3 position = elementTransformer(element);
                _entries[i] = new Entry(element, position);
                if (anyPoints)
                {
                    bounds.Encapsulate(position);
                }
                else
                {
                    bounds = new Bounds(position, Vector3.zero);
                    anyPoints = true;
                }

                _indices[i] = i;
            }

            if (anyPoints)
            {
                Vector3 size = bounds.size;
                const float minSize = 0.001f;
                if (size.x < minSize)
                {
                    size.x = minSize;
                }
                if (size.y < minSize)
                {
                    size.y = minSize;
                }
                if (size.z < minSize)
                {
                    size.z = minSize;
                }
                bounds.size = size;
            }

            _bounds = bounds;

            if (elementCount == 0)
            {
                _head = OctTreeNode.CreateLeaf(_bounds, 0, 0);
                return;
            }

            bucketSize = Math.Max(1, bucketSize);
            int[] scratch = ArrayPool<int>.Shared.Rent(elementCount);
            try
            {
                _head = BuildNode(_bounds, 0, elementCount, bucketSize, scratch);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(scratch, clearArray: true);
            }
        }

        private OctTreeNode BuildNode(
            Bounds boundary,
            int startIndex,
            int count,
            int bucketSize,
            int[] scratch
        )
        {
            if (count <= 0)
            {
                return OctTreeNode.CreateLeaf(boundary, startIndex, 0);
            }

            if (count <= bucketSize)
            {
                return OctTreeNode.CreateLeaf(boundary, startIndex, count);
            }

            Span<int> counts = stackalloc int[NumChildren];
            Span<int> starts = stackalloc int[NumChildren];
            Span<int> next = stackalloc int[NumChildren];

            Span<int> source = _indices.AsSpan(startIndex, count);
            Span<int> temp = scratch.AsSpan(0, count);

            Vector3 octantSize = boundary.size / 2f;
            Vector3 halfOctantSize = octantSize / 2f;
            Vector3 boundaryCenter = boundary.center;
            float centerX = boundaryCenter.x;
            float centerY = boundaryCenter.y;
            float centerZ = boundaryCenter.z;

            Entry[] entries = _entries;
            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector3 position = entries[entryIndex].position;
                bool east = position.x >= centerX;
                bool north = position.y >= centerY;
                bool up = position.z >= centerZ;
                int octant = (up ? 4 : 0) | (east ? 2 : 0) | (north ? 1 : 0);
                counts[octant]++;
            }

            int maxChildCount = 0;
            int running = 0;
            for (int q = 0; q < NumChildren; ++q)
            {
                starts[q] = running;
                next[q] = running;
                running += counts[q];
                if (counts[q] > maxChildCount)
                {
                    maxChildCount = counts[q];
                }
            }

            if (maxChildCount == count)
            {
                return OctTreeNode.CreateLeaf(boundary, startIndex, count);
            }

            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector3 position = entries[entryIndex].position;
                bool east = position.x >= centerX;
                bool north = position.y >= centerY;
                bool up = position.z >= centerZ;
                int octant = (up ? 4 : 0) | (east ? 2 : 0) | (north ? 1 : 0);
                int destination = next[octant]++;
                temp[destination] = entryIndex;
            }

            temp.CopyTo(source);

            Span<Bounds> octants = stackalloc Bounds[NumChildren];
            // Bottom layer (z-)
            octants[0] = new Bounds(
                new Vector3(
                    centerX - halfOctantSize.x,
                    centerY - halfOctantSize.y,
                    centerZ - halfOctantSize.z
                ),
                octantSize
            );
            octants[2] = new Bounds(
                new Vector3(
                    centerX + halfOctantSize.x,
                    centerY - halfOctantSize.y,
                    centerZ - halfOctantSize.z
                ),
                octantSize
            );
            octants[1] = new Bounds(
                new Vector3(
                    centerX - halfOctantSize.x,
                    centerY + halfOctantSize.y,
                    centerZ - halfOctantSize.z
                ),
                octantSize
            );
            octants[3] = new Bounds(
                new Vector3(
                    centerX + halfOctantSize.x,
                    centerY + halfOctantSize.y,
                    centerZ - halfOctantSize.z
                ),
                octantSize
            );
            // Top layer (z+)
            octants[4] = new Bounds(
                new Vector3(
                    centerX - halfOctantSize.x,
                    centerY - halfOctantSize.y,
                    centerZ + halfOctantSize.z
                ),
                octantSize
            );
            octants[6] = new Bounds(
                new Vector3(
                    centerX + halfOctantSize.x,
                    centerY - halfOctantSize.y,
                    centerZ + halfOctantSize.z
                ),
                octantSize
            );
            octants[5] = new Bounds(
                new Vector3(
                    centerX - halfOctantSize.x,
                    centerY + halfOctantSize.y,
                    centerZ + halfOctantSize.z
                ),
                octantSize
            );
            octants[7] = new Bounds(
                new Vector3(
                    centerX + halfOctantSize.x,
                    centerY + halfOctantSize.y,
                    centerZ + halfOctantSize.z
                ),
                octantSize
            );

            OctTreeNode[] children = new OctTreeNode[NumChildren];
            for (int q = 0; q < NumChildren; ++q)
            {
                int childCount = counts[q];
                if (childCount <= 0)
                {
                    continue;
                }

                int childStart = startIndex + starts[q];
                children[q] = BuildNode(octants[q], childStart, childCount, bucketSize, scratch);
            }

            return OctTreeNode.CreateInternal(boundary, children, startIndex, count);
        }

        public List<T> GetElementsInRange(
            Vector3 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0
        )
        {
            elementsInRange.Clear();
            if (range < 0f || _head._count <= 0)
            {
                return elementsInRange;
            }

            Bounds bounds = new(position, new Vector3(range * 2f, range * 2f, range * 2f));

            if (!bounds.Intersects(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<Stack<OctTreeNode>> nodesToVisitResource =
                Buffers<OctTreeNode>.Stack.Get();
            Stack<OctTreeNode> nodesToVisit = nodesToVisitResource.resource;
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;
            float rangeSquared = range * range;
            bool hasMinimumRange = 0f < minimumRange;
            float minimumRangeSquared = minimumRange * minimumRange;

            while (nodesToVisit.TryPop(out OctTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (!bounds.Intersects(currentNode.boundary))
                {
                    continue;
                }

                if (
                    currentNode.isTerminal
                    || (
                        bounds.Contains(currentNode.boundary.min)
                        && bounds.Contains(currentNode.boundary.max)
                    )
                )
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;
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

                    continue;
                }

                OctTreeNode[] childNodes = currentNode._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    OctTreeNode child = childNodes[i];
                    if (child is null || child._count <= 0)
                    {
                        continue;
                    }

                    if (bounds.Intersects(child.boundary))
                    {
                        nodesToVisit.Push(child);
                    }
                }
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            using PooledResource<Stack<OctTreeNode>> stackResource =
                Buffers<OctTreeNode>.Stack.Get();
            return GetElementsInBounds(bounds, elementsInBounds, stackResource.resource);
        }

        public List<T> GetElementsInBounds(
            Bounds bounds,
            List<T> elementsInBounds,
            Stack<OctTreeNode> nodeBuffer
        )
        {
            elementsInBounds.Clear();
            if (_head._count <= 0 || !bounds.Intersects(_bounds))
            {
                return elementsInBounds;
            }

            Stack<OctTreeNode> nodesToVisit = nodeBuffer ?? new Stack<OctTreeNode>();
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;

            while (nodesToVisit.TryPop(out OctTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (
                    bounds.Contains(currentNode.boundary.min)
                    && bounds.Contains(currentNode.boundary.max)
                )
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;
                    for (int i = start; i < end; ++i)
                    {
                        elementsInBounds.Add(entries[indices[i]].value);
                    }

                    continue;
                }

                if (currentNode.isTerminal)
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;
                    for (int i = start; i < end; ++i)
                    {
                        Entry entry = entries[indices[i]];
                        if (bounds.Contains(entry.position))
                        {
                            elementsInBounds.Add(entry.value);
                        }
                    }

                    continue;
                }

                OctTreeNode[] childNodes = currentNode._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    OctTreeNode child = childNodes[i];
                    if (child is null || child._count <= 0)
                    {
                        continue;
                    }

                    if (bounds.Intersects(child.boundary))
                    {
                        nodesToVisit.Push(child);
                    }
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

            Entry[] entries = _entries;
            int[] indices = _indices;

            using PooledResource<List<NodeDistance>> nodeHeapResource =
                Buffers<NodeDistance>.List.Get();
            List<NodeDistance> nodeHeap = nodeHeapResource.resource;
            nodeHeap.Clear();
            PushNode(nodeHeap, _head, position);

            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborBuffer = nearestNeighborBufferResource.resource;
            nearestNeighborBuffer.Clear();

            using PooledResource<List<Entry>> nearestNeighborsCacheResource =
                Buffers<Entry>.List.Get();
            List<Entry> nearestNeighborsCache = nearestNeighborsCacheResource.resource;
            nearestNeighborsCache.Clear();

            float currentWorstDistanceSquared = float.PositiveInfinity;

            while (nodeHeap.Count > 0)
            {
                NodeDistance best = PopNode(nodeHeap);

                if (
                    nearestNeighborBuffer.Count >= count
                    && best._distanceSquared >= currentWorstDistanceSquared
                )
                {
                    break;
                }

                OctTreeNode currentNode = best._node;

                if (!currentNode.isTerminal)
                {
                    OctTreeNode[] childNodes = currentNode._children;
                    for (int i = 0; i < childNodes.Length; ++i)
                    {
                        OctTreeNode child = childNodes[i];
                        if (child is not null && child._count > 0)
                        {
                            PushNode(nodeHeap, child, position);
                        }
                    }

                    continue;
                }

                int startIndex = currentNode._startIndex;
                int endIndex = startIndex + currentNode._count;
                for (int i = startIndex; i < endIndex; ++i)
                {
                    Entry entry = entries[indices[i]];
                    if (nearestNeighborBuffer.Add(entry.value))
                    {
                        nearestNeighborsCache.Add(entry);
                    }
                }

                if (nearestNeighborBuffer.Count >= count)
                {
                    currentWorstDistanceSquared = CalculateWorstDistanceSquared(
                        nearestNeighborsCache,
                        position
                    );
                }
            }

            nearestNeighborsCache.Sort(
                (lhs, rhs) =>
                    (lhs.position - position).sqrMagnitude.CompareTo(
                        (rhs.position - position).sqrMagnitude
                    )
            );

            nearestNeighbors.Clear();
            for (int i = 0; i < nearestNeighborsCache.Count && i < count; ++i)
            {
                nearestNeighbors.Add(nearestNeighborsCache[i].value);
            }

            return nearestNeighbors;

            static void PushNode(List<NodeDistance> heap, OctTreeNode node, Vector3 point)
            {
                NodeDistance entry = new NodeDistance(node, DistanceSquaredToBounds(node, point));
                heap.Add(entry);
                int index = heap.Count - 1;

                while (index > 0)
                {
                    int parent = (index - 1) >> 1;
                    NodeDistance parentEntry = heap[parent];
                    if (parentEntry._distanceSquared <= entry._distanceSquared)
                    {
                        break;
                    }

                    heap[index] = parentEntry;
                    index = parent;
                }

                heap[index] = entry;
            }

            static NodeDistance PopNode(List<NodeDistance> heap)
            {
                int lastIndex = heap.Count - 1;
                NodeDistance result = heap[0];
                NodeDistance last = heap[lastIndex];
                heap.RemoveAt(lastIndex);

                int index = 0;
                int count = heap.Count;
                while (true)
                {
                    int left = (index << 1) + 1;
                    if (left >= count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int smallest =
                        right < count && heap[right]._distanceSquared < heap[left]._distanceSquared
                            ? right
                            : left;

                    if (last._distanceSquared <= heap[smallest]._distanceSquared)
                    {
                        break;
                    }

                    heap[index] = heap[smallest];
                    index = smallest;
                }

                if (count > 0)
                {
                    heap[index] = last;
                }

                return result;
            }

            static float DistanceSquaredToBounds(OctTreeNode node, Vector3 point)
            {
                Bounds boundary = node.boundary;
                Vector3 closest = boundary.ClosestPoint(point);
                return (closest - point).sqrMagnitude;
            }

            static float CalculateWorstDistanceSquared(List<Entry> candidates, Vector3 point)
            {
                float worst = 0f;
                for (int i = 0; i < candidates.Count; ++i)
                {
                    float distanceSquared = (candidates[i].position - point).sqrMagnitude;
                    if (distanceSquared > worst)
                    {
                        worst = distanceSquared;
                    }
                }

                return worst;
            }
        }
    }
}
