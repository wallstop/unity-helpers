namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class RTree<T>
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
                foreach (
                    List<V> xSlice in elements
                        .Partition(rectanglesPerPagePerAxis)
                        .Select(enumerable => enumerable as List<V> ?? enumerable.ToList())
                )
                {
                    xSlice.Sort(YAxis);
                    foreach (
                        List<V> ySlice in xSlice
                            .Partition(intTargetSize)
                            .Select(enumerable => enumerable as List<V> ?? enumerable.ToList())
                    )
                    {
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
            Bounds bounds = elements.Select(elementTransformer).GetBounds() ?? new Bounds();

            // Ensure bounds have minimum size to handle colinear points
            // FastContains2D uses strict < for max bounds, so zero-size dimensions won't contain any points
            if (elements.Length > 0)
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
            _head = new RTreeNode<T>(
                elements.ToList(),
                elementTransformer,
                bucketSize,
                branchFactor
            );
        }

        public IEnumerable<T> GetElementsInRange(
            Vector2 position,
            float range,
            float minimumRange = 0f
        )
        {
            Circle area = new(position, range);
            if (0 < minimumRange)
            {
                Circle minimumArea = new(position, minimumRange);
                return GetElementsInBounds(
                        new Bounds(
                            new Vector3(position.x, position.y, 0f),
                            new Vector3(range * 2f, range * 2f, 1f)
                        )
                    )
                    .Where(element =>
                    {
                        Bounds elementBoundary = _elementTransformer(element);
                        if (!area.Intersects(elementBoundary))
                        {
                            return false;
                        }

                        return !minimumArea.Intersects(elementBoundary);
                    });
            }
            return GetElementsInBounds(
                    new Bounds(
                        new Vector3(position.x, position.y, 0f),
                        new Vector3(range * 2f, range * 2f, 1f)
                    )
                )
                .Where(element =>
                {
                    Bounds elementBoundary = _elementTransformer(element);
                    if (!area.Intersects(elementBoundary))
                    {
                        return false;
                    }

                    return true;
                });
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds)
        {
            using PooledResource<Stack<RTreeNode<T>>> nodeBufferResource = Buffers<
                RTreeNode<T>
            >.Stack.Get();
            return GetElementsInBounds(bounds, nodeBufferResource.resource);
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds, Stack<RTreeNode<T>> nodeBuffer)
        {
            if (!bounds.FastIntersects2D(_bounds))
            {
                yield break;
            }

            Stack<RTreeNode<T>> nodesToVisit = nodeBuffer ?? new Stack<RTreeNode<T>>();
            nodeBuffer.Clear();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out RTreeNode<T> currentNode))
            {
                if (currentNode.isTerminal)
                {
                    foreach (T element in currentNode.elements)
                    {
                        if (bounds.FastIntersects2D(_elementTransformer(element)))
                        {
                            yield return element;
                        }
                    }

                    continue;
                }

                // if (bounds.Overlaps2D(currentNode.boundary))
                // {
                //     foreach (T element in currentNode.elements)
                //     {
                //         yield return element;
                //     }
                //
                //     continue;
                // }

                foreach (RTreeNode<T> child in currentNode.children)
                {
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
        }

        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            using PooledResource<Stack<RTreeNode<T>>> nodeBufferResource = Buffers<
                RTreeNode<T>
            >.Stack.Get();
            Stack<RTreeNode<T>> nodeBuffer = nodeBufferResource.resource;
            using PooledResource<List<RTreeNode<T>>> childrenBufferResource = Buffers<
                RTreeNode<T>
            >.List.Get();
            List<RTreeNode<T>> childrenBuffer = childrenBufferResource.resource;
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborBuffer = nearestNeighborBufferResource.resource;
            GetApproximateNearestNeighbors(
                position,
                count,
                nearestNeighbors,
                nodeBuffer,
                childrenBuffer,
                nearestNeighborBuffer
            );
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors,
            Stack<RTreeNode<T>> nodeBuffer,
            List<RTreeNode<T>> childrenBuffer,
            HashSet<T> nearestNeighborsBuffer
        )
        {
            nearestNeighbors.Clear();

            RTreeNode<T> current = _head;
            Stack<RTreeNode<T>> stack = nodeBuffer ?? new Stack<RTreeNode<T>>();
            stack.Clear();
            stack.Push(_head);
            List<RTreeNode<T>> childrenCopy = childrenBuffer ?? new List<RTreeNode<T>>();
            childrenCopy.Clear();
            HashSet<T> nearestNeighborsSet = nearestNeighborsBuffer ?? new HashSet<T>(count);
            nearestNeighborsSet.Clear();

            Comparison<RTreeNode<T>> comparison = Comparison;
            while (!current.isTerminal)
            {
                childrenCopy.Clear();
                foreach (RTreeNode<T> child in current.children)
                {
                    childrenCopy.Add(child);
                }
                childrenCopy.Sort(comparison);
                for (int i = childrenCopy.Count - 1; 0 <= i; --i)
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
                foreach (T element in selected.elements)
                {
                    _ = nearestNeighborsSet.Add(element);
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

            return;

            int Comparison(RTreeNode<T> lhs, RTreeNode<T> rhs) =>
                ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(
                    ((Vector2)rhs.boundary.center - position).sqrMagnitude
                );
        }
    }
}
