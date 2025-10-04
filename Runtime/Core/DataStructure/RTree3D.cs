namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class RTree3D<T> : ISpatialTree3D<T>
    {
        internal const float MinimumNodeSize = 0.001f;

        [Serializable]
        internal struct ElementData
        {
            internal T _value;
            internal BoundingBox3D _bounds;
            internal Vector3 _center;
            internal uint _mortonKey;
        }

        [Serializable]
        public sealed class RTreeNode
        {
            public readonly BoundingBox3D boundary;
            internal readonly RTreeNode[] _children;
            internal readonly int _startIndex;
            internal readonly int _count;
            public readonly bool isTerminal;

            private RTreeNode(
                int startIndex,
                int count,
                BoundingBox3D boundary,
                RTreeNode[] children
            )
            {
                _startIndex = startIndex;
                _count = count;
                this.boundary = boundary;
                _children = children ?? Array.Empty<RTreeNode>();
                isTerminal = _children.Length == 0;
            }

            internal static RTreeNode CreateEmpty()
            {
                return new RTreeNode(0, 0, BoundingBox3D.Empty, Array.Empty<RTreeNode>());
            }

            internal static RTreeNode CreateLeaf(ElementData[] elements, int startIndex, int count)
            {
                BoundingBox3D nodeBounds = CalculateBounds(elements, startIndex, count);
                return new RTreeNode(startIndex, count, nodeBounds, Array.Empty<RTreeNode>());
            }

            internal static RTreeNode CreateInternal(RTreeNode[] children)
            {
                if (children.Length == 0)
                {
                    return CreateEmpty();
                }

                int startIndex = children[0]._startIndex;
                int lastChildIndex = children.Length - 1;
                RTreeNode lastChild = children[lastChildIndex];
                int endIndex = lastChild._startIndex + lastChild._count;
                BoundingBox3D nodeBounds = children[0].boundary;
                for (int i = 1; i < children.Length; ++i)
                {
                    nodeBounds = nodeBounds.ExpandToInclude(children[i].boundary);
                }

                nodeBounds = EnsureMinimumBounds(nodeBounds);
                return new RTreeNode(startIndex, endIndex - startIndex, nodeBounds, children);
            }
        }

        private readonly struct NodeDistance
        {
            internal readonly RTreeNode _node;
            internal readonly float _distanceSquared;

            internal NodeDistance(RTreeNode node, float distanceSquared)
            {
                _node = node;
                _distanceSquared = distanceSquared;
            }
        }

        public const int DefaultBucketSize = 10;
        public const int DefaultBranchFactor = 4;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds.ToBounds();

        private readonly BoundingBox3D _bounds;
        private readonly ElementData[] _elementData;
        private readonly RTreeNode _head;

        public RTree3D(
            IEnumerable<T> points,
            Func<T, Bounds> elementTransformer,
            int bucketSize = DefaultBucketSize,
            int branchFactor = DefaultBranchFactor
        )
        {
            elements =
                points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));

            Func<T, Bounds> transformer =
                elementTransformer ?? throw new ArgumentNullException(nameof(elementTransformer));

            int elementCount = elements.Length;
            _elementData = new ElementData[elementCount];
            ElementData[] elementData = _elementData;
            bucketSize = Math.Max(1, bucketSize);
            branchFactor = Math.Max(2, branchFactor);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            bool hasElements = false;

            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];

                Bounds elementBounds = transformer(element);
                BoundingBox3D elementBox = BoundingBox3D.FromClosedBounds(elementBounds);
                ElementData data = default;
                data._value = element;
                data._bounds = elementBox;
                data._center = elementBox.Center;
                elementData[i] = data;
                Vector3 min = elementBox.min;
                Vector3 max = elementBox.max;

                if (!hasElements)
                {
                    hasElements = true;
                }

                if (min.x < minX)
                {
                    minX = min.x;
                }
                if (min.y < minY)
                {
                    minY = min.y;
                }
                if (min.z < minZ)
                {
                    minZ = min.z;
                }
                if (max.x > maxX)
                {
                    maxX = max.x;
                }
                if (max.y > maxY)
                {
                    maxY = max.y;
                }
                if (max.z > maxZ)
                {
                    maxZ = max.z;
                }
            }

            BoundingBox3D bounds = hasElements
                ? new BoundingBox3D(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ))
                : BoundingBox3D.Empty;

            if (hasElements)
            {
                bounds = bounds.EnsureMinimumSize(MinimumNodeSize);
            }

            _bounds = bounds;
            if (!hasElements)
            {
                _head = RTreeNode.CreateEmpty();
                return;
            }

            float rangeX = maxX - minX;
            float rangeY = maxY - minY;
            float rangeZ = maxZ - minZ;
            float inverseRangeX = rangeX > float.Epsilon ? 1f / rangeX : 0f;
            float inverseRangeY = rangeY > float.Epsilon ? 1f / rangeY : 0f;
            float inverseRangeZ = rangeZ > float.Epsilon ? 1f / rangeZ : 0f;

            PooledResource<ulong[]> sortKeysResource = default;
            ulong[] sortKeys = null;
            if (elementCount > 1)
            {
                sortKeysResource = WallstopFastArrayPool<ulong>.Get(elementCount, out sortKeys);
            }

            for (int i = 0; i < elementCount; ++i)
            {
                ref ElementData data = ref elementData[i];
                Vector3 center = data._center;
                float normalizedX = (center.x - minX) * inverseRangeX;
                float normalizedY = (center.y - minY) * inverseRangeY;
                float normalizedZ = (center.z - minZ) * inverseRangeZ;
                ushort quantizedX = QuantizeNormalized(normalizedX);
                ushort quantizedY = QuantizeNormalized(normalizedY);
                ushort quantizedZ = QuantizeNormalized(normalizedZ);
                data._mortonKey = EncodeMorton(quantizedX, quantizedY, quantizedZ);
                if (sortKeys is not null)
                {
                    sortKeys[i] = ComposeSortKey(
                        data._mortonKey,
                        quantizedX,
                        quantizedY,
                        quantizedZ
                    );
                }
            }

            if (sortKeys is not null)
            {
                Array.Sort(sortKeys, elementData, 0, elementCount);
                sortKeysResource.Dispose();
            }

            List<RTreeNode> currentLevel = new(
                Math.Max(1, (int)Math.Ceiling(elementCount / (double)bucketSize))
            );
            for (int startIndex = 0; startIndex < elementCount; startIndex += bucketSize)
            {
                int count = Math.Min(bucketSize, elementCount - startIndex);
                currentLevel.Add(RTreeNode.CreateLeaf(elementData, startIndex, count));
            }

            while (currentLevel.Count > 1)
            {
                int parentCount = (currentLevel.Count + branchFactor - 1) / branchFactor;
                List<RTreeNode> nextLevel = new(parentCount);
                for (int i = 0; i < currentLevel.Count; i += branchFactor)
                {
                    int childCount = Math.Min(branchFactor, currentLevel.Count - i);
                    RTreeNode[] children = new RTreeNode[childCount];
                    currentLevel.CopyTo(i, children, 0, childCount);
                    nextLevel.Add(RTreeNode.CreateInternal(children));
                }

                currentLevel = nextLevel;
            }

            _head = currentLevel[0];
            _bounds = _head.boundary;
        }

        private void CollectElementIndicesInBounds(BoundingBox3D bounds, List<int> indices)
        {
            indices.Clear();
            if (!bounds.Intersects(_bounds))
            {
                return;
            }

            using PooledResource<Stack<RTreeNode>> nodeBufferResource =
                Buffers<RTreeNode>.Stack.Get();
            Stack<RTreeNode> nodesToVisit = nodeBufferResource.resource;
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out RTreeNode currentNode))
            {
                if (!bounds.Intersects(currentNode.boundary))
                {
                    continue;
                }

                if (currentNode.isTerminal)
                {
                    int start = currentNode._startIndex;
                    int end = start + currentNode._count;
                    for (int i = start; i < end; ++i)
                    {
                        ElementData elementData = _elementData[i];
                        if (bounds.Intersects(elementData._bounds))
                        {
                            indices.Add(i);
                        }
                    }

                    continue;
                }

                RTreeNode[] childNodes = currentNode._children;
                foreach (RTreeNode child in childNodes)
                {
                    if (child._count <= 0)
                    {
                        continue;
                    }

                    if (!bounds.Intersects(child.boundary))
                    {
                        continue;
                    }

                    nodesToVisit.Push(child);
                }
            }

            nodesToVisit.Clear();
        }

        public List<T> GetElementsInRange(
            Vector3 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0f
        )
        {
            elementsInRange.Clear();
            if (range <= 0f)
            {
                return elementsInRange;
            }

            BoundingBox3D queryBounds = BoundingBox3D.FromCenterAndSize(
                position,
                new Vector3(range * 2f, range * 2f, range * 2f)
            );

            if (!queryBounds.Intersects(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<List<int>> candidateIndicesResource = Buffers<int>.List.Get();
            List<int> candidateIndices = candidateIndicesResource.resource;
            CollectElementIndicesInBounds(queryBounds, candidateIndices);

            if (candidateIndices.Count == 0)
            {
                return elementsInRange;
            }

            Sphere area = new(position, range);
            bool hasMinimumRange = 0f < minimumRange;
            Sphere minimumArea = default;
            if (hasMinimumRange)
            {
                minimumArea = new Sphere(position, minimumRange);
            }

            foreach (int index in candidateIndices)
            {
                ElementData elementData = _elementData[index];
                BoundingBox3D elementBoundary = elementData._bounds;
                if (!area.Intersects(elementBoundary))
                {
                    continue;
                }

                if (hasMinimumRange && minimumArea.Intersects(elementBoundary))
                {
                    continue;
                }

                elementsInRange.Add(elementData._value);
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            elementsInBounds.Clear();
            BoundingBox3D queryBounds = BoundingBox3D.FromClosedBounds(bounds);
            if (!queryBounds.Intersects(_bounds))
            {
                return elementsInBounds;
            }

            using PooledResource<List<int>> indicesResource = Buffers<int>.List.Get();
            List<int> indices = indicesResource.resource;
            CollectElementIndicesInBounds(queryBounds, indices);
            foreach (int index in indices)
            {
                elementsInBounds.Add(_elementData[index]._value);
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

            using PooledResource<List<NodeDistance>> nodeHeapResource =
                Buffers<NodeDistance>.List.Get();
            List<NodeDistance> nodeHeap = nodeHeapResource.resource;
            nodeHeap.Clear();
            PushNode(nodeHeap, _head, position);

            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborsSet = nearestNeighborBufferResource.resource;
            nearestNeighborsSet.Clear();

            using PooledResource<List<int>> nearestIndexBufferResource = Buffers<int>.List.Get();
            List<int> nearestIndices = nearestIndexBufferResource.resource;
            nearestIndices.Clear();

            float currentWorstDistanceSquared = float.PositiveInfinity;

            while (nodeHeap.Count > 0)
            {
                NodeDistance best = PopNode(nodeHeap);

                if (
                    nearestNeighborsSet.Count >= count
                    && best._distanceSquared >= currentWorstDistanceSquared
                )
                {
                    break;
                }

                RTreeNode currentNode = best._node;

                if (!currentNode.isTerminal)
                {
                    RTreeNode[] childNodes = currentNode._children;
                    for (int i = 0; i < childNodes.Length; ++i)
                    {
                        RTreeNode child = childNodes[i];
                        if (child._count > 0)
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
                    ElementData elementData = _elementData[i];
                    if (!nearestNeighborsSet.Add(elementData._value))
                    {
                        continue;
                    }

                    nearestIndices.Add(i);
                }

                if (nearestNeighborsSet.Count >= count)
                {
                    currentWorstDistanceSquared = CalculateWorstDistanceSquared(
                        nearestIndices,
                        position
                    );
                }
            }

            if (nearestIndices.Count == 0)
            {
                nodeHeap.Clear();
                nearestNeighborsSet.Clear();
                nearestIndices.Clear();
                return nearestNeighbors;
            }

            {
                Vector3 localPosition = position;
                nearestIndices.Sort(
                    (lhsIndex, rhsIndex) =>
                    {
                        Vector3 lhsCenter = _elementData[lhsIndex]._center;
                        Vector3 rhsCenter = _elementData[rhsIndex]._center;
                        return (lhsCenter - localPosition).sqrMagnitude.CompareTo(
                            (rhsCenter - localPosition).sqrMagnitude
                        );
                    }
                );

                if (nearestIndices.Count > count)
                {
                    nearestIndices.RemoveRange(count, nearestIndices.Count - count);
                }
            }

            foreach (int index in nearestIndices)
            {
                nearestNeighbors.Add(_elementData[index]._value);
            }

            nodeHeap.Clear();
            nearestNeighborsSet.Clear();
            nearestIndices.Clear();

            return nearestNeighbors;

            static void PushNode(List<NodeDistance> heap, RTreeNode node, Vector3 point)
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

            float CalculateWorstDistanceSquared(List<int> indices, Vector3 point)
            {
                float worst = 0f;
                for (int i = 0; i < indices.Count; ++i)
                {
                    Vector3 center = _elementData[indices[i]]._center;
                    float distanceSquared = (center - point).sqrMagnitude;
                    if (distanceSquared > worst)
                    {
                        worst = distanceSquared;
                    }
                }

                return worst;
            }
        }

        private static BoundingBox3D CalculateBounds(
            ElementData[] elements,
            int startIndex,
            int count
        )
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; ++i)
            {
                BoundingBox3D bounds = elements[i]._bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                minX = Math.Min(minX, min.x);
                maxX = Math.Max(maxX, max.x);
                minY = Math.Min(minY, min.y);
                maxY = Math.Max(maxY, max.y);
                minZ = Math.Min(minZ, min.z);
                maxZ = Math.Max(maxZ, max.z);
            }

            BoundingBox3D nodeBounds = new BoundingBox3D(
                new Vector3(minX, minY, minZ),
                new Vector3(maxX, maxY, maxZ)
            );

            return EnsureMinimumBounds(nodeBounds);
        }

        private static BoundingBox3D EnsureMinimumBounds(BoundingBox3D bounds)
        {
            return bounds.EnsureMinimumSize(MinimumNodeSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint EncodeMorton(ushort quantizedX, ushort quantizedY, ushort quantizedZ)
        {
            uint mortonX = Part1By2(quantizedX);
            uint mortonY = Part1By2(quantizedY);
            uint mortonZ = Part1By2(quantizedZ);
            return mortonX | (mortonY << 1) | (mortonZ << 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort QuantizeNormalized(float normalized)
        {
            if (normalized <= 0f)
            {
                return 0;
            }

            if (normalized >= 1f)
            {
                return 1023;
            }

            return (ushort)(normalized * 1023f + 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ComposeSortKey(
            uint mortonKey,
            ushort quantizedX,
            ushort quantizedY,
            ushort quantizedZ
        )
        {
            return ((ulong)mortonKey << 32)
                | ((ulong)quantizedX << 20)
                | ((ulong)quantizedY << 10)
                | quantizedZ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Part1By2(uint value)
        {
            value &= 0x000003ff;
            value = (value | (value << 16)) & 0xFF0000FF;
            value = (value | (value << 8)) & 0x0F00F00F;
            value = (value | (value << 4)) & 0xC30C30C3;
            value = (value | (value << 2)) & 0x49249249;
            return value;
        }
    }
}
