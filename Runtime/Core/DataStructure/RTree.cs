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
        [Serializable]
        public sealed class RTreeNode<V>
        {
            public readonly Bounds boundary;
            internal readonly RTreeNode<V>[] children;
            public readonly V[] elements;
            public readonly bool isTerminal;

            public RTreeNode(
                List<V> elements,
                Func<V, Bounds> elementTransformer,
                int bucketSize,
                int branchFactor
            )
            {
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                foreach (V element in elements)
                {
                    Bounds rectangle = elementTransformer(element);
                    Vector3 min = rectangle.min;
                    Vector3 max = rectangle.max;
                    minX = Math.Min(minX, min.x);
                    maxX = Math.Max(maxX, max.x);
                    minY = Math.Min(minY, min.y);
                    maxY = Math.Max(maxY, max.y);
                }

                Bounds bounds =
                    elements.Count <= 0
                        ? new Bounds()
                        : new Bounds(
                            new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2),
                            new Vector3(maxX - minX, maxY - minY)
                        );

                // Ensure bounds have minimum size to handle colinear points
                // FastContains2D uses strict < for max bounds, so zero-size dimensions won't contain any points
                if (elements.Count > 0)
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
                    bounds.size = size;
                }

                boundary = bounds;
                this.elements = elements.ToArray();
                isTerminal = elements.Count <= bucketSize;
                if (isTerminal)
                {
                    children = Array.Empty<RTreeNode<V>>();
                    return;
                }

                /*
                    http://www.dtic.mil/get-tr-doc/pdf?AD=ADA324493
                    var targetSize = rectangles.Count / (double) branchFactor;
                    P = branchFactor;
                    S = Math.sqrt(P);
                    N = targetSize;

                    Ugh.
                */
                double targetSize = elements.Count / (double)branchFactor;
                int intTargetSize = (int)Math.Ceiling(targetSize);

                List<RTreeNode<V>> tempChildren = new(intTargetSize);

                double slicesPerAxis = Math.Sqrt(branchFactor);
                int rectanglesPerPagePerAxis = (int)(slicesPerAxis * targetSize);

                elements.Sort(XAxis);

                int xSliceSize = Math.Max(1, rectanglesPerPagePerAxis);
                int ySliceSize = Math.Max(1, intTargetSize);

                using PooledResource<List<V>> xSliceResource = Buffers<V>.List.Get();
                List<V> xSlice = xSliceResource.resource;
                using PooledResource<List<V>> ySliceResource = Buffers<V>.List.Get();
                List<V> ySlice = ySliceResource.resource;

                for (int startIndex = 0; startIndex < elements.Count; startIndex += xSliceSize)
                {
                    xSlice.Clear();
                    int xCount = Math.Min(xSliceSize, elements.Count - startIndex);
                    for (int i = 0; i < xCount; ++i)
                    {
                        xSlice.Add(elements[startIndex + i]);
                    }

                    xSlice.Sort(YAxis);

                    for (int yStart = 0; yStart < xSlice.Count; yStart += ySliceSize)
                    {
                        ySlice.Clear();
                        int yCount = Math.Min(ySliceSize, xSlice.Count - yStart);
                        for (int i = 0; i < yCount; ++i)
                        {
                            ySlice.Add(xSlice[yStart + i]);
                        }

                        RTreeNode<V> node = new(
                            ySlice,
                            elementTransformer,
                            bucketSize,
                            branchFactor
                        );
                        tempChildren.Add(node);
                    }
                }

                children = tempChildren.ToArray();
                return;

                int XAxis(V lhs, V rhs)
                {
                    return elementTransformer(lhs)
                        .center.x.CompareTo(elementTransformer(rhs).center.x);
                }

                int YAxis(V lhs, V rhs)
                {
                    return elementTransformer(lhs)
                        .center.y.CompareTo(elementTransformer(rhs).center.y);
                }
            }
        }

        public const int DefaultBucketSize = 10;
        public const int DefaultBranchFactor = 4;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Func<T, Bounds> _elementTransformer;
        private readonly RTreeNode<T> _head;

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
            List<T> elementList = new(elementCount);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool hasElements = false;

            for (int i = 0; i < elementCount; ++i)
            {
                T element = elements[i];
                elementList.Add(element);

                Bounds elementBounds = elementTransformer(element);
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
                const float minSize = 0.001f;
                if (size.x < minSize)
                {
                    size.x = minSize;
                }
                if (size.y < minSize)
                {
                    size.y = minSize;
                }
                bounds.size = size;
            }

            _bounds = bounds;
            _head = new RTreeNode<T>(elementList, elementTransformer, bucketSize, branchFactor);
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

            using PooledResource<Stack<RTreeNode<T>>> nodeBufferResource = Buffers<
                RTreeNode<T>
            >.Stack.Get();
            Stack<RTreeNode<T>> nodesToVisit = nodeBufferResource.resource;
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out RTreeNode<T> currentNode))
            {
                if (currentNode.isTerminal)
                {
                    T[] nodeElements = currentNode.elements;
                    for (int i = 0; i < nodeElements.Length; ++i)
                    {
                        T element = nodeElements[i];
                        if (bounds.FastIntersects2D(_elementTransformer(element)))
                        {
                            elementsInBounds.Add(element);
                        }
                    }

                    continue;
                }

                RTreeNode<T>[] childNodes = currentNode.children;
                for (int i = 0; i < childNodes.Length; ++i)
                {
                    RTreeNode<T> child = childNodes[i];
                    if (child.elements.Length <= 0)
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

            if (count <= 0 || _head.elements.Length == 0)
            {
                return nearestNeighbors;
            }

            RTreeNode<T> current = _head;

            using PooledResource<List<RTreeNode<T>>> childrenBufferResource = Buffers<
                RTreeNode<T>
            >.List.Get();
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            using PooledResource<Stack<RTreeNode<T>>> nodeBufferResource = Buffers<
                RTreeNode<T>
            >.Stack.Get();
            Stack<RTreeNode<T>> stack = nodeBufferResource.resource;
            stack.Push(_head);
            List<RTreeNode<T>> childrenCopy = childrenBufferResource.resource;
            HashSet<T> nearestNeighborsSet = nearestNeighborBufferResource.resource;

            Comparison<RTreeNode<T>> comparison = Comparison;
            while (!current.isTerminal)
            {
                childrenCopy.Clear();
                RTreeNode<T>[] childNodes = current.children;
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
                if (current.elements.Length <= count)
                {
                    break;
                }
            }

            while (nearestNeighborsSet.Count < count && stack.TryPop(out RTreeNode<T> selected))
            {
                T[] nodeElements = selected.elements;
                for (int i = 0; i < nodeElements.Length; ++i)
                {
                    nearestNeighborsSet.Add(nodeElements[i]);
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

            int Comparison(RTreeNode<T> lhs, RTreeNode<T> rhs) =>
                ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(
                    ((Vector2)rhs.boundary.center - position).sqrMagnitude
                );
        }
    }
}
