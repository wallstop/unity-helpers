namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class KDTree3D<T> : ISpatialTree3D<T>
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
        private const int SmallPartitionThreshold = 32;

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Entry[] _entries;
        private readonly int[] _indices;
        private readonly KDTreeNode _head;
        private readonly bool _balanced;
        private readonly int _bucketSize;

        public KDTree3D(
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

        private KDTreeNode BuildBalanced(int startIndex, int count, int depth)
        {
            if (count <= _bucketSize)
            {
                Bounds leafBounds = CalculateLeafBounds(startIndex, count);
                return KDTreeNode.CreateLeaf(leafBounds, startIndex, count);
            }

            int axis = depth % 3;

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

        private KDTreeNode BuildUnbalanced(int startIndex, int count, int axis, int[] scratch)
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
                return KDTreeNode.CreateLeaf(nodeBounds, startIndex, count);
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
                return KDTreeNode.CreateLeaf(nodeBounds, startIndex, count);
            }

            temp.CopyTo(source);

            int nextAxis = (axis + 1) % 3;
            KDTreeNode left = BuildUnbalanced(startIndex, leftCount, nextAxis, scratch);
            KDTreeNode right = BuildUnbalanced(
                startIndex + leftCount,
                rightCount,
                nextAxis,
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
            if (range < 0f || _head.count <= 0)
            {
                return elementsInRange;
            }

            Bounds bounds = new(position, new Vector3(range * 2f, range * 2f, range * 2f));

            if (!bounds.Intersects(_bounds))
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

                if (!bounds.Intersects(currentNode.boundary))
                {
                    continue;
                }

                if (
                    currentNode.isTerminal
                    || bounds.Contains(currentNode.boundary.min)
                        && bounds.Contains(currentNode.boundary.max)
                )
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
                if (left is not null && left.count > 0 && bounds.Intersects(left.boundary))
                {
                    nodesToVisit.Push(left);
                }

                KDTreeNode right = currentNode.right;
                if (right is not null && right.count > 0 && bounds.Intersects(right.boundary))
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
            if (_head.count <= 0 || !bounds.Intersects(_bounds))
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

                if (
                    bounds.Contains(currentNode.boundary.min)
                    && bounds.Contains(currentNode.boundary.max)
                )
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
                        if (bounds.Contains(entry.position))
                        {
                            elementsInBounds.Add(entry.value);
                        }
                    }

                    continue;
                }

                KDTreeNode left = currentNode.left;
                if (left is not null && left.count > 0 && bounds.Intersects(left.boundary))
                {
                    nodesToVisit.Push(left);
                }

                KDTreeNode right = currentNode.right;
                if (right is not null && right.count > 0 && bounds.Intersects(right.boundary))
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
            nearestNeighborsCache.Clear();

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

                float leftDistance = (left.boundary.center - position).sqrMagnitude;
                float rightDistance = (right.boundary.center - position).sqrMagnitude;
                if (leftDistance < rightDistance)
                {
                    // Push the sibling as well to widen candidate pool
                    if (right is not null && right.count > 0)
                    {
                        nodeBuffer.Push(right);
                    }

                    nodeBuffer.Push(left);
                    current = left;
                    if (left.count <= count)
                    {
                        break;
                    }
                }
                else
                {
                    // Push the sibling as well to widen candidate pool
                    if (left is not null && left.count > 0)
                    {
                        nodeBuffer.Push(left);
                    }

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

            // Always sort by proximity to ensure closest candidates are selected
            {
                Vector3 localPosition = position;
                nearestNeighborsCache.Sort(NearestComparison);

                int NearestComparison(Entry lhs, Entry rhs) =>
                    (lhs.position - localPosition).sqrMagnitude.CompareTo(
                        (rhs.position - localPosition).sqrMagnitude
                    );
            }

            // Trim to requested count
            nearestNeighbors.Clear();
            for (int i = 0; i < nearestNeighborsCache.Count && i < count; ++i)
            {
                nearestNeighbors.Add(nearestNeighborsCache[i].value);
            }

            return nearestNeighbors;
        }
    }
}
