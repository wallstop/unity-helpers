namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class QuadTree2D<T> : ISpatialTree2D<T>
    {
        private const int NumChildren = 4;

        [Serializable]
        public readonly struct Entry
        {
            public readonly T value;
            public readonly Vector2 position;

            public Entry(T value, Vector2 position)
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
        public sealed class QuadTreeNode
        {
            public readonly Bounds boundary;
            internal readonly QuadTreeNode[] _children;
            internal readonly int _startIndex;
            internal readonly int _count;
            public readonly bool isTerminal;

            private QuadTreeNode(
                Bounds boundary,
                int startIndex,
                int count,
                bool isTerminal,
                QuadTreeNode[] children
            )
            {
                this.boundary = boundary;
                _startIndex = startIndex;
                _count = count;
                this.isTerminal = isTerminal;
                _children = children ?? Array.Empty<QuadTreeNode>();
            }

            internal static QuadTreeNode CreateLeaf(Bounds boundary, int startIndex, int count)
            {
                return new QuadTreeNode(
                    boundary,
                    startIndex,
                    count,
                    true,
                    Array.Empty<QuadTreeNode>()
                );
            }

            internal static QuadTreeNode CreateInternal(
                Bounds boundary,
                QuadTreeNode[] children,
                int startIndex,
                int count
            )
            {
                return new QuadTreeNode(boundary, startIndex, count, false, children);
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Entry[] _entries;
        private readonly int[] _indices;
        private readonly QuadTreeNode _head;

        public QuadTree2D(
            IEnumerable<T> points,
            Func<T, Vector2> elementTransformer,
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
                Vector2 position = elementTransformer(element);
                _entries[i] = new Entry(element, position);
                if (anyPoints)
                {
                    bounds.Encapsulate(position);
                }
                else
                {
                    bounds = new Bounds(position, new Vector3(0f, 0f, 1f));
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
                size.z = 1f;
                bounds.size = size;
            }

            _bounds = bounds;

            if (elementCount == 0)
            {
                _head = QuadTreeNode.CreateLeaf(_bounds, 0, 0);
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

        public QuadTree2D(
            IEnumerable<Entry> entries,
            Bounds? boundary = null,
            int bucketSize = DefaultBucketSize
        )
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            using PooledResource<List<Entry>> entryListResource = Buffers<Entry>.List.Get(
                out List<Entry> entryList
            );
            foreach (Entry entry in entries)
            {
                entryList.Add(entry);
            }

            int elementCount = entryList.Count;
            if (elementCount == 0)
            {
                elements = ImmutableArray<T>.Empty;
                _entries = Array.Empty<Entry>();
                _indices = Array.Empty<int>();
                _bounds = boundary ?? default;
                _head = QuadTreeNode.CreateLeaf(_bounds, 0, 0);
                return;
            }

            _entries = new Entry[elementCount];
            _indices = new int[elementCount];
            ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>(elementCount);
            Bounds bounds = boundary ?? default;
            bool anyPoints = boundary.HasValue;
            for (int i = 0; i < elementCount; ++i)
            {
                Entry entry = entryList[i];
                _entries[i] = entry;
                builder.Add(entry.value);
                Vector2 position = entry.position;
                if (anyPoints)
                {
                    bounds.Encapsulate(position);
                }
                else
                {
                    bounds = new Bounds(position, new Vector3(0f, 0f, 1f));
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

                size.z = 1f;
                bounds.size = size;
            }

            elements = builder.MoveToImmutable();
            _bounds = bounds;
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

        private QuadTreeNode BuildNode(
            Bounds boundary,
            int startIndex,
            int count,
            int bucketSize,
            int[] scratch
        )
        {
            if (count <= 0)
            {
                return QuadTreeNode.CreateLeaf(boundary, startIndex, 0);
            }

            if (count <= bucketSize)
            {
                return QuadTreeNode.CreateLeaf(boundary, startIndex, count);
            }

            Span<int> counts = stackalloc int[NumChildren];
            Span<int> starts = stackalloc int[NumChildren];
            Span<int> next = stackalloc int[NumChildren];

            Span<int> source = _indices.AsSpan(startIndex, count);
            Span<int> temp = scratch.AsSpan(0, count);

            Vector3 quadrantSize = boundary.size / 2f;
            quadrantSize.z = 1f;
            Vector3 halfQuadrantSize = quadrantSize / 2f;
            Vector3 boundaryCenter = boundary.center;
            float centerX = boundaryCenter.x;
            float centerY = boundaryCenter.y;

            Entry[] entries = _entries;
            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector2 position = entries[entryIndex].position;
                bool east = position.x > centerX;
                bool north = position.y >= centerY;
                int quadrant = east ? (north ? 1 : 2) : (north ? 0 : 3);
                counts[quadrant]++;
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
                return QuadTreeNode.CreateLeaf(boundary, startIndex, count);
            }

            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector2 position = entries[entryIndex].position;
                bool east = position.x > centerX;
                bool north = position.y >= centerY;
                int quadrant = east ? (north ? 1 : 2) : (north ? 0 : 3);
                int destination = next[quadrant]++;
                temp[destination] = entryIndex;
            }

            temp.CopyTo(source);

            Span<Bounds> quadrants = stackalloc Bounds[NumChildren];
            quadrants[0] = new Bounds(
                new Vector3(centerX - halfQuadrantSize.x, centerY + halfQuadrantSize.y),
                quadrantSize
            );
            quadrants[1] = new Bounds(
                new Vector3(centerX + halfQuadrantSize.x, centerY + halfQuadrantSize.y),
                quadrantSize
            );
            quadrants[2] = new Bounds(
                new Vector3(centerX + halfQuadrantSize.x, centerY - halfQuadrantSize.y),
                quadrantSize
            );
            quadrants[3] = new Bounds(
                new Vector3(centerX - halfQuadrantSize.x, centerY - halfQuadrantSize.y),
                quadrantSize
            );

            QuadTreeNode[] children = new QuadTreeNode[NumChildren];
            for (int q = 0; q < NumChildren; ++q)
            {
                int childCount = counts[q];
                if (childCount <= 0)
                {
                    continue;
                }

                int childStart = startIndex + starts[q];
                children[q] = BuildNode(quadrants[q], childStart, childCount, bucketSize, scratch);
            }

            return QuadTreeNode.CreateInternal(boundary, children, startIndex, count);
        }

        public List<T> GetElementsInRange(
            Vector2 position,
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

            Bounds bounds = new(position, new Vector3(range * 2f, range * 2f, 1f));

            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<Stack<QuadTreeNode>> nodesToVisitResource =
                Buffers<QuadTreeNode>.Stack.Get();
            Stack<QuadTreeNode> nodesToVisit = nodesToVisitResource.resource;
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;
            float rangeSquared = range * range;
            bool hasMinimumRange = 0f < minimumRange;
            float minimumRangeSquared = minimumRange * minimumRange;

            while (nodesToVisit.TryPop(out QuadTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (!bounds.FastIntersects2D(currentNode.boundary))
                {
                    continue;
                }

                if (currentNode.isTerminal || bounds.FastContains2D(currentNode.boundary))
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

                QuadTreeNode[] childNodes = currentNode._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    QuadTreeNode child = childNodes[i];
                    if (child is null || child._count <= 0)
                    {
                        continue;
                    }

                    if (bounds.FastIntersects2D(child.boundary))
                    {
                        nodesToVisit.Push(child);
                    }
                }
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            elementsInBounds.Clear();
            if (_head._count <= 0 || !bounds.FastIntersects2D(_bounds))
            {
                return elementsInBounds;
            }
            using PooledResource<Stack<QuadTreeNode>> stackResource =
                Buffers<QuadTreeNode>.Stack.Get(out Stack<QuadTreeNode> nodesToVisit);
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;

            while (nodesToVisit.TryPop(out QuadTreeNode currentNode))
            {
                if (currentNode is null || currentNode._count <= 0)
                {
                    continue;
                }

                if (bounds.FastContains2D(currentNode.boundary))
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
                        if (bounds.FastContains2D(entry.position))
                        {
                            elementsInBounds.Add(entry.value);
                        }
                    }

                    continue;
                }

                QuadTreeNode[] childNodes = currentNode._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    QuadTreeNode child = childNodes[i];
                    if (child is null || child._count <= 0)
                    {
                        continue;
                    }

                    if (bounds.FastIntersects2D(child.boundary))
                    {
                        nodesToVisit.Push(child);
                    }
                }
            }

            return elementsInBounds;
        }

        public List<T> GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            nearestNeighbors.Clear();

            if (count <= 0 || _head._count == 0)
            {
                return nearestNeighbors;
            }

            using PooledResource<Stack<QuadTreeNode>> nodeBufferResource =
                Buffers<QuadTreeNode>.Stack.Get();
            Stack<QuadTreeNode> nodeBuffer = nodeBufferResource.resource;
            nodeBuffer.Push(_head);
            using PooledResource<List<QuadTreeNode>> childrenBufferResource =
                Buffers<QuadTreeNode>.List.Get();
            List<QuadTreeNode> childrenBuffer = childrenBufferResource.resource;
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborBuffer = nearestNeighborBufferResource.resource;
            using PooledResource<List<Neighbor>> neighborCandidatesResource =
                Buffers<Neighbor>.List.Get();
            List<Neighbor> neighborCandidates = neighborCandidatesResource.resource;

            Entry[] entries = _entries;
            int[] indices = _indices;
            Vector2 searchPosition = position;

            QuadTreeNode current = _head;

            while (!current.isTerminal)
            {
                childrenBuffer.Clear();
                QuadTreeNode[] childNodes = current._children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    QuadTreeNode child = childNodes[i];
                    if (child is not null && child._count > 0)
                    {
                        childrenBuffer.Add(child);
                    }
                }

                if (childrenBuffer.Count == 0)
                {
                    break;
                }

                SortChildrenByDistance(childrenBuffer, searchPosition);
                for (int i = childrenBuffer.Count - 1; i >= 0; --i)
                {
                    nodeBuffer.Push(childrenBuffer[i]);
                }

                current = childrenBuffer[0];
                if (current._count <= count)
                {
                    break;
                }
            }

            while (
                nearestNeighborBuffer.Count < count && nodeBuffer.TryPop(out QuadTreeNode selected)
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

                    float sqrDistance = (entry.position - searchPosition).sqrMagnitude;
                    neighborCandidates.Add(new Neighbor(entry.value, sqrDistance));
                }
            }

            if (count < neighborCandidates.Count)
            {
                neighborCandidates.Sort(NeighborComparer.Instance);
                neighborCandidates.RemoveRange(count, neighborCandidates.Count - count);
            }

            nearestNeighbors.Clear();
            for (int i = 0; i < neighborCandidates.Count && i < count; ++i)
            {
                nearestNeighbors.Add(neighborCandidates[i].value);
            }

            return nearestNeighbors;
        }

        private static void SortChildrenByDistance(List<QuadTreeNode> nodes, Vector2 searchPosition)
        {
            for (int i = 1; i < nodes.Count; ++i)
            {
                QuadTreeNode value = nodes[i];
                float valueDistance = GetSqrDistance(value, searchPosition);
                int j = i - 1;
                while (j >= 0 && valueDistance < GetSqrDistance(nodes[j], searchPosition))
                {
                    nodes[j + 1] = nodes[j];
                    --j;
                }

                nodes[j + 1] = value;
            }
        }

        private static float GetSqrDistance(QuadTreeNode node, Vector2 position)
        {
            return ((Vector2)node.boundary.center - position).sqrMagnitude;
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
