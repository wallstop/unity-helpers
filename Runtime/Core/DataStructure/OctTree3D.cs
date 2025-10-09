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
            public readonly BoundingBox3D boundary;
            internal readonly OctTreeNode[] _children;
            internal readonly int _startIndex;
            internal readonly int _count;
            public readonly bool isTerminal;

            private OctTreeNode(
                BoundingBox3D boundary,
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

            internal static OctTreeNode CreateLeaf(
                BoundingBox3D boundary,
                int startIndex,
                int count
            )
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
                BoundingBox3D boundary,
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

        private readonly struct EntryDistance
        {
            internal readonly Entry entry;
            internal readonly float distanceSquared;

            internal EntryDistance(Entry entry, float distanceSquared)
            {
                this.entry = entry;
                this.distanceSquared = distanceSquared;
            }
        }

        private sealed class EntryDistanceComparer : IComparer<EntryDistance>
        {
            internal static readonly EntryDistanceComparer Instance = new();

            public int Compare(EntryDistance x, EntryDistance y)
            {
                return x.distanceSquared.CompareTo(y.distanceSquared);
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _boundary;

        private readonly BoundingBox3D _bounds;
        private readonly Bounds _boundary;
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

            BoundingBox3D bounds = boundary.HasValue
                ? BoundingBox3D.FromClosedBounds(boundary.Value)
                : BoundingBox3D.Empty;
            bool anyPoints = boundary.HasValue;

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
                    minX = position.x;
                if (position.y < minY)
                    minY = position.y;
                if (position.z < minZ)
                    minZ = position.z;
                if (position.x > maxX)
                    maxX = position.x;
                if (position.y > maxY)
                    maxY = position.y;
                if (position.z > maxZ)
                    maxZ = position.z;
                if (anyPoints)
                {
                    bounds = bounds.ExpandToInclude(position);
                }
                else
                {
                    bounds = BoundingBox3D.FromPoint(position);
                    anyPoints = true;
                }

                _indices[i] = i;
            }

            if (anyPoints)
            {
                bounds = bounds.EnsureMinimumSize(0.001f);
            }

            _bounds = bounds;

            if (elementCount == 0)
            {
                _boundary = new Bounds();
            }
            else
            {
                Vector3 closedMin = new(minX, minY, minZ);
                Vector3 closedMax = new(maxX, maxY, maxZ);
                Vector3 center = (closedMin + closedMax) * 0.5f;
                Vector3 size = closedMax - closedMin;
                _boundary = new Bounds(center, size);
            }

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
            BoundingBox3D boundary,
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

            Vector3 boundaryMin = boundary.min;
            Vector3 boundaryMax = boundary.max;
            Vector3 boundaryCenter = boundary.Center;
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

            float minX = boundaryMin.x;
            float minY = boundaryMin.y;
            float minZ = boundaryMin.z;
            float maxX = boundaryMax.x;
            float maxY = boundaryMax.y;
            float maxZ = boundaryMax.z;

            Span<BoundingBox3D> octants = stackalloc BoundingBox3D[NumChildren];
            // Bottom layer (z-)
            octants[0] = new BoundingBox3D(
                new Vector3(minX, minY, minZ),
                new Vector3(centerX, centerY, centerZ)
            );
            octants[2] = new BoundingBox3D(
                new Vector3(centerX, minY, minZ),
                new Vector3(maxX, centerY, centerZ)
            );
            octants[1] = new BoundingBox3D(
                new Vector3(minX, centerY, minZ),
                new Vector3(centerX, maxY, centerZ)
            );
            octants[3] = new BoundingBox3D(
                new Vector3(centerX, centerY, minZ),
                new Vector3(maxX, maxY, centerZ)
            );
            // Top layer (z+)
            octants[4] = new BoundingBox3D(
                new Vector3(minX, minY, centerZ),
                new Vector3(centerX, centerY, maxZ)
            );
            octants[6] = new BoundingBox3D(
                new Vector3(centerX, minY, centerZ),
                new Vector3(maxX, centerY, maxZ)
            );
            octants[5] = new BoundingBox3D(
                new Vector3(minX, centerY, centerZ),
                new Vector3(centerX, maxY, maxZ)
            );
            octants[7] = new BoundingBox3D(
                new Vector3(centerX, centerY, centerZ),
                new Vector3(maxX, maxY, maxZ)
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

            Sphere querySphere = new(position, range);
            BoundingBox3D queryBounds = BoundingBox3D.FromCenterAndSize(
                position,
                new Vector3(range * 2f, range * 2f, range * 2f)
            );

            if (!queryBounds.Intersects(_bounds))
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
            Sphere minimumSphere = hasMinimumRange ? new Sphere(position, minimumRange) : default;

            while (nodesToVisit.TryPop(out OctTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (!queryBounds.Intersects(currentNode.boundary))
                {
                    continue;
                }

                // Use Sphere.Overlaps to check if the sphere fully contains the node's boundary
                // This is more accurate than using a bounding box approximation
                bool nodeFullyContained = querySphere.Overlaps(currentNode.boundary);

                if (currentNode.isTerminal || nodeFullyContained)
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;

                    // If the node is fully contained, we can skip distance checks for points
                    // but still need to check minimum range
                    if (nodeFullyContained && !hasMinimumRange)
                    {
                        // Fast path: all points in this node are within range
                        for (int i = start; i < end; ++i)
                        {
                            elementsInRange.Add(entries[indices[i]].value);
                        }
                    }
                    else if (nodeFullyContained && hasMinimumRange)
                    {
                        // Node is fully in outer sphere, but need to check minimum range
                        // Check if node is fully outside minimum sphere
                        bool nodeFullyOutsideMinimum = !minimumSphere.Intersects(
                            currentNode.boundary
                        );
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

                OctTreeNode[] childNodes = currentNode._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    OctTreeNode child = childNodes[i];
                    if (child is null || child._count <= 0)
                    {
                        continue;
                    }

                    if (IntersectsOrTouches(queryBounds, child.boundary))
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
            // Use inclusive-max conversion to match Unity Bounds.Contains semantics
            BoundingBox3D query = BoundingBox3D.FromClosedBoundsInclusiveMax(bounds);
            return GetElementsInBounds(query, elementsInBounds, stackResource.resource);
        }

        public List<T> GetElementsInBounds(
            BoundingBox3D queryBounds,
            List<T> elementsInBounds,
            Stack<OctTreeNode> nodeBuffer
        )
        {
            elementsInBounds.Clear();
            if (_head._count <= 0 || !IntersectsOrTouches(queryBounds, _bounds))
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

                if (queryBounds.Contains(currentNode.boundary))
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
                        if (queryBounds.Contains(entry.position))
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

                    if (IntersectsOrTouches(queryBounds, child.boundary))
                    {
                        nodesToVisit.Push(child);
                    }
                }
            }

            return elementsInBounds;
        }

        private static bool IntersectsOrTouches(BoundingBox3D a, BoundingBox3D b)
        {
            return a.min.x <= b.max.x
                && a.max.x >= b.min.x
                && a.min.y <= b.max.y
                && a.max.y >= b.min.y
                && a.min.z <= b.max.z
                && a.max.z >= b.min.z;
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

            using PooledResource<List<EntryDistance>> bestNeighborResource =
                Buffers<EntryDistance>.List.Get();
            List<EntryDistance> bestNeighbors = bestNeighborResource.resource;
            bestNeighbors.Clear();

            using PooledResource<HashSet<T>> bestNeighborValuesResource = Buffers<T>.HashSet.Get();
            HashSet<T> bestNeighborValues = bestNeighborValuesResource.resource;
            bestNeighborValues.Clear();

            float currentWorstDistanceSquared = float.PositiveInfinity;

            while (nodeHeap.Count > 0)
            {
                NodeDistance best = PopNode(nodeHeap);

                if (
                    bestNeighbors.Count >= count
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
                    float distanceSquared = (entry.position - position).sqrMagnitude;

                    if (bestNeighbors.Count < count)
                    {
                        if (!bestNeighborValues.Add(entry.value))
                        {
                            continue;
                        }

                        bestNeighbors.Add(new EntryDistance(entry, distanceSquared));

                        if (bestNeighbors.Count == count)
                        {
                            currentWorstDistanceSquared = FindWorstDistanceSquared(bestNeighbors);
                        }

                        continue;
                    }

                    if (distanceSquared >= currentWorstDistanceSquared)
                    {
                        continue;
                    }

                    if (bestNeighborValues.Contains(entry.value))
                    {
                        continue;
                    }

                    EntryDistance replaced = ReplaceWorstNeighbor(
                        bestNeighbors,
                        entry,
                        distanceSquared
                    );
                    bestNeighborValues.Remove(replaced.entry.value);
                    bestNeighborValues.Add(entry.value);
                    currentWorstDistanceSquared = FindWorstDistanceSquared(bestNeighbors);
                }
            }

            if (bestNeighbors.Count > 1)
            {
                bestNeighbors.Sort(EntryDistanceComparer.Instance);
            }

            nearestNeighbors.Clear();
            for (int i = 0; i < bestNeighbors.Count && i < count; ++i)
            {
                nearestNeighbors.Add(bestNeighbors[i].entry.value);
            }

            return nearestNeighbors;

            static void PushNode(List<NodeDistance> heap, OctTreeNode node, Vector3 point)
            {
                NodeDistance entry = new NodeDistance(node, node.boundary.DistanceSquaredTo(point));
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

            static float FindWorstDistanceSquared(List<EntryDistance> candidates)
            {
                float worst = 0f;
                for (int i = 0; i < candidates.Count; ++i)
                {
                    float distance = candidates[i].distanceSquared;
                    if (distance > worst)
                    {
                        worst = distance;
                    }
                }

                return worst;
            }

            static EntryDistance ReplaceWorstNeighbor(
                List<EntryDistance> candidates,
                Entry entry,
                float distanceSquared
            )
            {
                int worstIndex = 0;
                float worstDistance = candidates[0].distanceSquared;

                for (int i = 1; i < candidates.Count; ++i)
                {
                    float candidateDistance = candidates[i].distanceSquared;
                    if (candidateDistance > worstDistance)
                    {
                        worstDistance = candidateDistance;
                        worstIndex = i;
                    }
                }

                EntryDistance replaced = candidates[worstIndex];
                candidates[worstIndex] = new EntryDistance(entry, distanceSquared);
                return replaced;
            }
        }
    }
}
