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
    public sealed class KDTree2D<T> : ISpatialTree2D<T>
    {
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

        [Serializable]
        public sealed class KDTreeNode
        {
            public readonly Bounds boundary;
            public readonly KDTreeNode left;
            public readonly KDTreeNode right;
            internal readonly int startIndex;
            internal readonly int count;
            public readonly bool isTerminal;

            private KDTreeNode(
                Bounds boundary,
                KDTreeNode left,
                KDTreeNode right,
                int startIndex,
                int count,
                bool isTerminal
            )
            {
                this.boundary = boundary;
                this.left = left;
                this.right = right;
                this.startIndex = startIndex;
                this.count = count;
                this.isTerminal = isTerminal;
            }

            internal static KDTreeNode CreateLeaf(Bounds boundary, int startIndex, int count)
            {
                return new KDTreeNode(boundary, null, null, startIndex, count, true);
            }

            internal static KDTreeNode CreateInternal(
                Bounds boundary,
                KDTreeNode left,
                KDTreeNode right,
                int startIndex,
                int count
            )
            {
                return new KDTreeNode(boundary, left, right, startIndex, count, false);
            }
        }

        private const float MinimumNodeSize = 0.001f;

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Entry[] _entries;
        private readonly int[] _indices;
        private readonly KDTreeNode _head;
        private readonly bool _balanced;
        private readonly int _bucketSize;

        public KDTree2D(
            IEnumerable<T> points,
            Func<T, Vector2> elementTransformer,
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

            Bounds bounds = default;
            bool boundsInitialized = false;
            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];
                Vector2 position = elementTransformer(element);
                _entries[i] = new Entry(element, position);
                if (boundsInitialized)
                {
                    bounds.Encapsulate(position);
                }
                else
                {
                    bounds = new Bounds(position, new Vector3(0f, 0f, 1f));
                    boundsInitialized = true;
                }

                _indices[i] = i;
            }

            if (boundsInitialized)
            {
                EnsureMinimumBounds(ref bounds);
            }

            if (elementCount == 0)
            {
                _bounds = bounds;
                _head = KDTreeNode.CreateLeaf(_bounds, 0, 0);
                return;
            }

            KDTreeNode root;
            if (_balanced)
            {
                root = BuildBalanced(0, elementCount, depth: 0);
            }
            else
            {
                int[] scratch = ArrayPool<int>.Shared.Rent(elementCount);
                try
                {
                    root = BuildUnbalanced(0, elementCount, splitOnXAxis: true, scratch);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(scratch, clearArray: true);
                }
            }

            _head = root;
            _bounds = root.boundary;
        }

        private KDTreeNode BuildBalanced(int startIndex, int count, int depth)
        {
            if (count <= _bucketSize)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KDTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            bool splitOnXAxis = (depth & 1) == 0;
            int axis = splitOnXAxis ? 0 : 1;

            Span<int> span = _indices.AsSpan(startIndex, count);
            int leftCount = count / 2;
            if (leftCount == 0)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KDTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            SelectKth(span, leftCount, axis);
            int rightCount = count - leftCount;
            if (rightCount == 0)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KDTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            KDTreeNode left = BuildBalanced(startIndex, leftCount, depth + 1);
            KDTreeNode right = BuildBalanced(startIndex + leftCount, rightCount, depth + 1);
            Bounds boundary = CombineChildBounds(left.boundary, right.boundary);
            return KDTreeNode.CreateInternal(boundary, left, right, startIndex, count);
        }

        private KDTreeNode BuildUnbalanced(
            int startIndex,
            int count,
            bool splitOnXAxis,
            int[] scratch
        )
        {
            Span<int> source = _indices.AsSpan(startIndex, count);
            Span<int> temp = scratch.AsSpan(0, count);
            Entry[] entries = _entries;

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < count; ++i)
            {
                Vector2 position = entries[source[i]].position;
                if (position.x < minX)
                {
                    minX = position.x;
                }
                if (position.y < minY)
                {
                    minY = position.y;
                }
                if (position.x > maxX)
                {
                    maxX = position.x;
                }
                if (position.y > maxY)
                {
                    maxY = position.y;
                }
            }

            Bounds nodeBounds = CreateBounds(minX, maxX, minY, maxY);

            if (count <= _bucketSize)
            {
                return KDTreeNode.CreateLeaf(nodeBounds, startIndex, count);
            }

            float cutoff = splitOnXAxis ? nodeBounds.center.x : nodeBounds.center.y;

            int leftWrite = 0;
            int rightWrite = count - 1;
            for (int i = 0; i < count; ++i)
            {
                int entryIndex = source[i];
                Vector2 position = entries[entryIndex].position;
                float value = splitOnXAxis ? position.x : position.y;
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
                return KDTreeNode.CreateLeaf(nodeBounds, startIndex, count);
            }

            temp.CopyTo(source);

            KDTreeNode left = BuildUnbalanced(startIndex, leftCount, !splitOnXAxis, scratch);
            KDTreeNode right = BuildUnbalanced(
                startIndex + leftCount,
                rightCount,
                !splitOnXAxis,
                scratch
            );
            Bounds boundary = CombineChildBounds(left.boundary, right.boundary);
            return KDTreeNode.CreateInternal(boundary, left, right, startIndex, count);
        }

        private Bounds CalculateLeafBounds(int startIndex, int count)
        {
            if (count <= 0)
            {
                return new Bounds();
            }

            Entry[] entries = _entries;
            int[] indices = _indices;
            Bounds bounds = new Bounds(
                entries[indices[startIndex]].position,
                new Vector3(0f, 0f, 1f)
            );
            for (int i = 1; i < count; ++i)
            {
                bounds.Encapsulate(entries[indices[startIndex + i]].position);
            }

            EnsureMinimumBounds(ref bounds);
            return bounds;
        }

        private void SelectKth(Span<int> span, int k, int axis)
        {
            Entry[] entries = _entries;
            int left = 0;
            int right = span.Length - 1;

            while (left <= right)
            {
                if (left == right)
                {
                    return;
                }

                int pivotIndex = left + ((right - left) >> 1);
                (int lower, int upper) = Partition(span, left, right, pivotIndex, axis, entries);

                if (k < lower)
                {
                    right = lower - 1;
                    continue;
                }

                if (k < upper)
                {
                    return;
                }

                left = upper;
            }
        }

        private static (int lowerBound, int upperBound) Partition(
            Span<int> span,
            int left,
            int right,
            int pivotIndex,
            int axis,
            Entry[] entries
        )
        {
            int pivotEntryIndex = span[pivotIndex];
            float pivotValue = GetAxis(entries[pivotEntryIndex], axis);
            (span[pivotIndex], span[right]) = (span[right], span[pivotIndex]);

            int storeIndex = left;
            for (int i = left; i < right; ++i)
            {
                if (GetAxis(entries[span[i]], axis) < pivotValue)
                {
                    (span[storeIndex], span[i]) = (span[i], span[storeIndex]);
                    storeIndex++;
                }
            }

            int storeIndexEq = storeIndex;
            for (int i = storeIndex; i < right; ++i)
            {
                if (GetAxis(entries[span[i]], axis) == pivotValue)
                {
                    (span[storeIndexEq], span[i]) = (span[i], span[storeIndexEq]);
                    storeIndexEq++;
                }
            }

            (span[right], span[storeIndexEq]) = (span[storeIndexEq], span[right]);
            storeIndexEq++;

            return (storeIndex, storeIndexEq);
        }

        private static float GetAxis(in Entry entry, int axis) =>
            axis == 0 ? entry.position.x : entry.position.y;

        private static Bounds CombineChildBounds(Bounds left, Bounds right)
        {
            Bounds combined = left;
            combined.Encapsulate(right);
            EnsureMinimumBounds(ref combined);
            return combined;
        }

        private static Bounds CreateBounds(float minX, float maxX, float minY, float maxY)
        {
            if (float.IsInfinity(minX) || float.IsInfinity(minY))
            {
                return new Bounds();
            }

            Vector3 min = new(minX, minY, 0f);
            Vector3 max = new(maxX, maxY, 0f);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            Bounds bounds = new(center, new Vector3(size.x, size.y, 1f));
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
            size.z = 1f;
            bounds.size = size;
        }

        public List<T> GetElementsInRange(
            Vector2 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0
        )
        {
            elementsInRange.Clear();
            // Allow zero range to return only exact matches (distance == 0)
            if (range < 0f || _head.count <= 0)
            {
                return elementsInRange;
            }

            Bounds bounds = new(position, new Vector3(range * 2f, range * 2f, 1f));

            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<Stack<KDTreeNode>> stackResource = Buffers<KDTreeNode>.Stack.Get();
            Stack<KDTreeNode> nodesToVisit = stackResource.resource;
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;
            float rangeSquared = range * range;
            bool hasMinimumRange = 0f < minimumRange;
            float minimumRangeSquared = minimumRange * minimumRange;

            while (nodesToVisit.TryPop(out KDTreeNode currentNode))
            {
                if (currentNode is null || currentNode.count <= 0)
                {
                    continue;
                }

                if (!bounds.FastIntersects2D(currentNode.boundary))
                {
                    continue;
                }

                if (currentNode.isTerminal || bounds.FastContains2D(currentNode.boundary))
                {
                    int start = currentNode.startIndex;
                    int end = start + currentNode.count;
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

                KDTreeNode left = currentNode.left;
                if (left is not null && left.count > 0 && bounds.FastIntersects2D(left.boundary))
                {
                    nodesToVisit.Push(left);
                }

                KDTreeNode right = currentNode.right;
                if (right is not null && right.count > 0 && bounds.FastIntersects2D(right.boundary))
                {
                    nodesToVisit.Push(right);
                }
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            using PooledResource<Stack<KDTreeNode>> stackResource = Buffers<KDTreeNode>.Stack.Get();
            return GetElementsInBounds(bounds, elementsInBounds, stackResource.resource);
        }

        public List<T> GetElementsInBounds(
            Bounds bounds,
            List<T> elementsInBounds,
            Stack<KDTreeNode> nodeBuffer
        )
        {
            elementsInBounds.Clear();
            if (_head.count <= 0 || !bounds.FastIntersects2D(_bounds))
            {
                return elementsInBounds;
            }

            Stack<KDTreeNode> nodesToVisit = nodeBuffer ?? new Stack<KDTreeNode>();
            nodesToVisit.Clear();
            nodesToVisit.Push(_head);

            Entry[] entries = _entries;
            int[] indices = _indices;

            while (nodesToVisit.TryPop(out KDTreeNode currentNode))
            {
                if (currentNode is null || currentNode.count <= 0)
                {
                    continue;
                }

                if (bounds.FastContains2D(currentNode.boundary))
                {
                    int start = currentNode.startIndex;
                    int end = start + currentNode.count;
                    for (int i = start; i < end; ++i)
                    {
                        elementsInBounds.Add(entries[indices[i]].value);
                    }

                    continue;
                }

                if (currentNode.isTerminal)
                {
                    int start = currentNode.startIndex;
                    int end = start + currentNode.count;
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

                KDTreeNode left = currentNode.left;
                if (left is not null && left.count > 0 && bounds.FastIntersects2D(left.boundary))
                {
                    nodesToVisit.Push(left);
                }

                KDTreeNode right = currentNode.right;
                if (right is not null && right.count > 0 && bounds.FastIntersects2D(right.boundary))
                {
                    nodesToVisit.Push(right);
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

            if (count <= 0 || _head.count == 0)
            {
                return nearestNeighbors;
            }

            KDTreeNode current = _head;

            using PooledResource<Stack<KDTreeNode>> nodeBufferResource =
                Buffers<KDTreeNode>.Stack.Get();
            Stack<KDTreeNode> nodeBuffer = nodeBufferResource.resource;
            nodeBuffer.Push(_head);
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborBuffer = nearestNeighborBufferResource.resource;
            using PooledResource<List<Entry>> nearestNeighborsCacheResource =
                Buffers<Entry>.List.Get();
            List<Entry> nearestNeighborsCache = nearestNeighborsCacheResource.resource;

            Entry[] entries = _entries;
            int[] indices = _indices;

            while (!current.isTerminal)
            {
                KDTreeNode left = current.left;
                KDTreeNode right = current.right;
                if (left is null || left.count == 0)
                {
                    if (right is null || right.count == 0)
                    {
                        break;
                    }

                    nodeBuffer.Push(right);
                    current = right;
                    if (right.count <= count)
                    {
                        break;
                    }

                    continue;
                }

                if (right is null || right.count == 0)
                {
                    nodeBuffer.Push(left);
                    current = left;
                    if (left.count <= count)
                    {
                        break;
                    }

                    continue;
                }

                float leftDistance = ((Vector2)left.boundary.center - position).sqrMagnitude;
                float rightDistance = ((Vector2)right.boundary.center - position).sqrMagnitude;
                if (leftDistance < rightDistance)
                {
                    nodeBuffer.Push(left);
                    current = left;
                    if (left.count <= count)
                    {
                        break;
                    }
                }
                else
                {
                    nodeBuffer.Push(right);
                    current = right;
                    if (right.count <= count)
                    {
                        break;
                    }
                }
            }

            while (
                nearestNeighborBuffer.Count < count && nodeBuffer.TryPop(out KDTreeNode selected)
            )
            {
                if (selected is null || selected.count <= 0)
                {
                    continue;
                }

                int start = selected.startIndex;
                int end = start + selected.count;
                for (int i = start; i < end; ++i)
                {
                    Entry entry = entries[indices[i]];
                    if (nearestNeighborBuffer.Add(entry.value))
                    {
                        nearestNeighborsCache.Add(entry);
                    }
                }
            }

            if (count < nearestNeighborsCache.Count)
            {
                Vector2 localPosition = position;
                nearestNeighborsCache.Sort(NearestComparison);

                int NearestComparison(Entry lhs, Entry rhs) =>
                    (lhs.position - localPosition).sqrMagnitude.CompareTo(
                        (rhs.position - localPosition).sqrMagnitude
                    );
            }

            nearestNeighbors.Clear();
            for (int i = 0; i < nearestNeighborsCache.Count && i < count; ++i)
            {
                nearestNeighbors.Add(nearestNeighborsCache[i].value);
            }

            return nearestNeighbors;
        }
    }
}
