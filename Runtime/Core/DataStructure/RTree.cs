namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class RTree<T> : ISpatialTree<T>
    {
        internal const float MinimumNodeSize = 0.001f;

        [Serializable]
        internal readonly struct ElementData
        {
            internal ElementData(T value, Bounds bounds)
            {
                Value = value;
                Bounds = bounds;
                Center = bounds.center;
            }

            internal T Value { get; }
            internal Bounds Bounds { get; }
            internal Vector2 Center { get; }
        }

        [Serializable]
        public sealed class RTreeNode
        {
            public readonly Bounds boundary;
            internal readonly RTreeNode[] children;
            internal readonly int startIndex;
            internal readonly int count;
            public readonly bool isTerminal;

            internal RTreeNode(
                ElementData[] elements,
                int startIndex,
                int count,
                int bucketSize,
                int branchFactor
            )
            {
                this.startIndex = startIndex;
                this.count = count;

                if (count <= 0)
                {
                    boundary = new Bounds();
                    children = Array.Empty<RTreeNode>();
                    isTerminal = true;
                    return;
                }

                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                int endIndex = startIndex + count;
                for (int i = startIndex; i < endIndex; ++i)
                {
                    Bounds rectangle = elements[i].Bounds;
                    Vector3 min = rectangle.min;
                    Vector3 max = rectangle.max;
                    minX = Math.Min(minX, min.x);
                    maxX = Math.Max(maxX, max.x);
                    minY = Math.Min(minY, min.y);
                    maxY = Math.Max(maxY, max.y);
                }

                Bounds bounds = new Bounds(
                    new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2),
                    new Vector3(maxX - minX, maxY - minY)
                );

                Vector3 size = bounds.size;
                if (size.x < MinimumNodeSize)
                {
                    size.x = MinimumNodeSize;
                }
                if (size.y < MinimumNodeSize)
                {
                    size.y = MinimumNodeSize;
                }
                bounds.size = size;

                boundary = bounds;
                isTerminal = count <= bucketSize;
                if (isTerminal)
                {
                    children = Array.Empty<RTreeNode>();
                    return;
                }

                double targetSize = count / (double)branchFactor;
                int intTargetSize = Math.Max(1, (int)Math.Ceiling(targetSize));

                List<RTreeNode> tempChildren = new(branchFactor);

                double slicesPerAxis = Math.Sqrt(branchFactor);
                int rectanglesPerPagePerAxis = Math.Max(1, (int)(slicesPerAxis * targetSize));

                Array.Sort(elements, startIndex, count, XAxisComparer.Instance);

                int xSliceSize = Math.Max(1, rectanglesPerPagePerAxis);
                int ySliceSize = Math.Max(1, intTargetSize);

                for (int xStart = startIndex; xStart < endIndex; xStart += xSliceSize)
                {
                    int xCount = Math.Min(xSliceSize, endIndex - xStart);
                    Array.Sort(elements, xStart, xCount, YAxisComparer.Instance);

                    int xSliceEnd = xStart + xCount;
                    for (int yStart = xStart; yStart < xSliceEnd; yStart += ySliceSize)
                    {
                        int yCount = Math.Min(ySliceSize, xSliceEnd - yStart);
                        RTreeNode node = new(elements, yStart, yCount, bucketSize, branchFactor);
                        tempChildren.Add(node);
                    }
                }

                children = tempChildren.ToArray();
            }

            private sealed class XAxisComparer : IComparer<ElementData>
            {
                internal static readonly XAxisComparer Instance = new();

                public int Compare(ElementData lhs, ElementData rhs)
                {
                    return lhs.Center.x.CompareTo(rhs.Center.x);
                }
            }

            private sealed class YAxisComparer : IComparer<ElementData>
            {
                internal static readonly YAxisComparer Instance = new();

                public int Compare(ElementData lhs, ElementData rhs)
                {
                    return lhs.Center.y.CompareTo(rhs.Center.y);
                }
            }
        }

        public const int DefaultBucketSize = 10;
        public const int DefaultBranchFactor = 4;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Func<T, Bounds> _elementTransformer;
        private readonly ElementData[] _elementData;
        private readonly RTreeNode _head;

        public RTree(
            IEnumerable<T> points,
            Func<T, Bounds> elementTransformer,
            int bucketSize = DefaultBucketSize,
            int branchFactor = DefaultBranchFactor
        )
        {
            _elementTransformer =
                elementTransformer ?? throw new ArgumentNullException(nameof(elementTransformer));
            elements =
                points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));

            int elementCount = elements.Length;
            _elementData = new ElementData[elementCount];
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool hasElements = false;

            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];

                Bounds elementBounds = elementTransformer(element);
                _elementData[i] = new ElementData(element, elementBounds);
                Vector3 min = elementBounds.min;
                Vector3 max = elementBounds.max;

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
                if (max.x > maxX)
                {
                    maxX = max.x;
                }
                if (max.y > maxY)
                {
                    maxY = max.y;
                }
            }

            Bounds bounds = hasElements
                ? new Bounds(
                    new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2),
                    new Vector3(maxX - minX, maxY - minY)
                )
                : new Bounds();

            // Ensure bounds have minimum size to handle colinear points
            // FastContains2D uses strict < for max bounds, so zero-size dimensions won't contain any points
            if (hasElements)
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
                bounds.size = size;
            }

            _bounds = bounds;
            _head = new RTreeNode(_elementData, 0, elementCount, bucketSize, branchFactor);
        }

        public List<T> GetElementsInRange(
            Vector2 position,
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

            Bounds queryBounds = new(
                new Vector3(position.x, position.y, 0f),
                new Vector3(range * 2f, range * 2f, 1f)
            );

            if (!queryBounds.FastIntersects2D(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<List<T>> candidatesResource = Buffers<T>.List.Get();
            List<T> candidates = candidatesResource.resource;
            GetElementsInBounds(queryBounds, candidates);

            if (candidates.Count == 0)
            {
                return elementsInRange;
            }

            Circle area = new(position, range);
            bool hasMinimumRange = 0f < minimumRange;
            Circle minimumArea = default;
            if (hasMinimumRange)
            {
                minimumArea = new Circle(position, minimumRange);
            }

            for (int i = 0; i < candidates.Count; ++i)
            {
                T element = candidates[i];
                Bounds elementBoundary = _elementTransformer(element);
                if (!area.Intersects(elementBoundary))
                {
                    continue;
                }

                if (hasMinimumRange && minimumArea.Intersects(elementBoundary))
                {
                    continue;
                }

                elementsInRange.Add(element);
            }

            return elementsInRange;
        }

        public List<T> GetElementsInBounds(Bounds bounds, List<T> elementsInBounds)
        {
            elementsInBounds.Clear();
            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInBounds;
            }

            using PooledResource<Stack<RTreeNode>> nodeBufferResource =
                Buffers<RTreeNode>.Stack.Get();
            Stack<RTreeNode> nodesToVisit = nodeBufferResource.resource;
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out RTreeNode currentNode))
            {
                if (currentNode.isTerminal)
                {
                    int start = currentNode.startIndex;
                    int end = start + currentNode.count;
                    for (int i = start; i < end; ++i)
                    {
                        ElementData elementData = _elementData[i];
                        if (bounds.FastIntersects2D(elementData.Bounds))
                        {
                            elementsInBounds.Add(elementData.Value);
                        }
                    }

                    continue;
                }

                RTreeNode[] childNodes = currentNode.children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    RTreeNode child = childNodes[i];
                    if (child.count <= 0)
                    {
                        continue;
                    }

                    if (!bounds.FastIntersects2D(child.boundary))
                    {
                        continue;
                    }

                    nodesToVisit.Push(child);
                }
            }

            nodesToVisit.Clear();
            return elementsInBounds;
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
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

            RTreeNode current = _head;

            using PooledResource<List<RTreeNode>> childrenBufferResource =
                Buffers<RTreeNode>.List.Get();
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            using PooledResource<Stack<RTreeNode>> nodeBufferResource =
                Buffers<RTreeNode>.Stack.Get();
            Stack<RTreeNode> stack = nodeBufferResource.resource;
            stack.Push(_head);
            List<RTreeNode> childrenCopy = childrenBufferResource.resource;
            HashSet<T> nearestNeighborsSet = nearestNeighborBufferResource.resource;

            Comparison<RTreeNode> comparison = Comparison;
            while (!current.isTerminal)
            {
                childrenCopy.Clear();
                RTreeNode[] childNodes = current.children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    childrenCopy.Add(childNodes[i]);
                }

                if (childrenCopy.Count == 0)
                {
                    break;
                }

                childrenCopy.Sort(comparison);
                for (int i = childrenCopy.Count - 1; i >= 0; --i)
                {
                    stack.Push(childrenCopy[i]);
                }

                current = childrenCopy[0];
                if (current.count <= count)
                {
                    break;
                }
            }

            while (nearestNeighborsSet.Count < count && stack.TryPop(out RTreeNode selected))
            {
                int start = selected.startIndex;
                int end = start + selected.count;
                for (int i = start; i < end; ++i)
                {
                    nearestNeighborsSet.Add(_elementData[i].Value);
                }
            }

            foreach (T element in nearestNeighborsSet)
            {
                nearestNeighbors.Add(element);
            }

            if (count < nearestNeighbors.Count)
            {
                Vector2 localPosition = position;
                nearestNeighbors.Sort(NearestComparison);
                nearestNeighbors.RemoveRange(count, nearestNeighbors.Count - count);

                int NearestComparison(T lhs, T rhs) =>
                    (
                        (Vector2)_elementTransformer(lhs).center - localPosition
                    ).sqrMagnitude.CompareTo(
                        ((Vector2)_elementTransformer(rhs).center - localPosition).sqrMagnitude
                    );
            }

            stack.Clear();
            childrenCopy.Clear();
            nearestNeighborsSet.Clear();

            return nearestNeighbors;

            int Comparison(RTreeNode lhs, RTreeNode rhs) =>
                ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(
                    ((Vector2)rhs.boundary.center - position).sqrMagnitude
                );
        }
    }
}
