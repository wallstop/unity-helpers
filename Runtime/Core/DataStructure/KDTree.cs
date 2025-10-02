namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class KDTree<T> : ISpatialTree<T>
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
            public readonly Entry[] entries;
            public readonly bool isTerminal;

            public KDTreeNode(List<Entry> elements, int bucketSize, bool isXAxis, bool balanced)
            {
                bool initializedBoundary = false;
                Bounds bounds = new();
                foreach (Entry element in elements)
                {
                    if (initializedBoundary)
                    {
                        bounds.Encapsulate(element.position);
                    }
                    else
                    {
                        bounds = new Bounds(element.position, new Vector3(0f, 0f, 1f));
                    }

                    initializedBoundary = true;
                }

                // Ensure bounds have minimum size to handle colinear points
                // FastContains2D uses strict < for max bounds, so zero-size dimensions won't contain any points
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

                boundary = bounds;
                entries = 0 < elements.Count ? elements.ToArray() : Array.Empty<Entry>();
                isTerminal = elements.Count <= bucketSize;
                if (isTerminal)
                {
                    return;
                }

                if (balanced)
                {
                    if (isXAxis)
                    {
                        Array.Sort(entries, (lhs, rhs) => lhs.position.x.CompareTo(rhs.position.x));
                    }
                    else
                    {
                        Array.Sort(entries, (lhs, rhs) => lhs.position.y.CompareTo(rhs.position.y));
                    }

                    int cutoff = elements.Count / 2;

                    List<Entry> leftList = new();
                    List<Entry> rightList = new();
                    for (int i = 0; i < entries.Length; ++i)
                    {
                        Entry element = entries[i];
                        if (i < cutoff)
                        {
                            leftList.Add(element);
                        }
                        else
                        {
                            rightList.Add(element);
                        }
                    }

                    left = new KDTreeNode(leftList, bucketSize, !isXAxis, true);
                    right = new KDTreeNode(rightList, bucketSize, !isXAxis, true);
                }
                else
                {
                    Vector2 cutoff = boundary.center;
                    if (isXAxis)
                    {
                        List<Entry> leftList = new();
                        List<Entry> rightList = new();
                        foreach (Entry element in entries)
                        {
                            if (element.position.x <= cutoff.x)
                            {
                                leftList.Add(element);
                            }
                            else
                            {
                                rightList.Add(element);
                            }
                        }
                        left = new KDTreeNode(leftList, bucketSize, false, false);
                        right = new KDTreeNode(rightList, bucketSize, false, false);
                    }
                    else
                    {
                        List<Entry> leftList = new();
                        List<Entry> rightList = new();
                        foreach (Entry element in entries)
                        {
                            if (element.position.y <= cutoff.y)
                            {
                                leftList.Add(element);
                            }
                            else
                            {
                                rightList.Add(element);
                            }
                        }
                        left = new KDTreeNode(leftList, bucketSize, true, false);
                        right = new KDTreeNode(rightList, bucketSize, true, false);
                    }
                }
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly KDTreeNode _head;

        public KDTree(
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
            Bounds bounds = new();
            bool boundsInitialized = false;
            List<Entry> entries = new();
            foreach (T element in elements)
            {
                Vector2 elementPosition = elementTransformer(element);
                if (boundsInitialized)
                {
                    bounds.Encapsulate(elementPosition);
                }
                else
                {
                    bounds = new Bounds(elementPosition, new Vector3(0f, 0f, 1f));
                }
                boundsInitialized = true;
                entries.Add(new Entry(element, elementPosition));
            }
            // Ensure bounds have minimum size to handle colinear points
            // FastContains2D uses strict < for max bounds, so zero-size dimensions won't contain any points
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

            _bounds = bounds;
            _head = new KDTreeNode(
                entries,
                bucketSize: bucketSize,
                isXAxis: true,
                balanced: balanced
            );
        }

        public List<T> GetElementsInRange(
            Vector2 position,
            float range,
            List<T> elementsInRange,
            float minimumRange = 0
        )
        {
            elementsInRange.Clear();
            Bounds bounds = new(position, new Vector3(range * 2, range * 2, 1f));

            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInRange;
            }

            using PooledResource<Stack<KDTreeNode>> stackResource = Buffers<KDTreeNode>.Stack.Get();
            Stack<KDTreeNode> nodesToVisit = stackResource.resource;
            nodesToVisit.Push(_head);

            using PooledResource<List<KDTreeNode>> resultResource = Buffers<KDTreeNode>.List.Get();
            List<KDTreeNode> resultBuffer = resultResource.resource;

            while (nodesToVisit.TryPop(out KDTreeNode currentNode))
            {
                if (currentNode.isTerminal || bounds.Overlaps2D(currentNode.boundary))
                {
                    resultBuffer.Add(currentNode);
                    continue;
                }

                KDTreeNode leftNode = currentNode.left;
                if (0 < leftNode.entries.Length && bounds.FastIntersects2D(leftNode.boundary))
                {
                    nodesToVisit.Push(leftNode);
                }

                KDTreeNode rightNode = currentNode.right;
                if (0 < rightNode.entries.Length && bounds.FastIntersects2D(rightNode.boundary))
                {
                    nodesToVisit.Push(rightNode);
                }
            }

            if (0 < minimumRange)
            {
                float minimumRangeSquared = minimumRange * minimumRange;
                float rangeSquared = range * range;
                foreach (KDTreeNode node in resultBuffer)
                {
                    foreach (Entry element in node.entries)
                    {
                        float squareDistance = (element.position - position).sqrMagnitude;
                        if (squareDistance <= minimumRangeSquared || rangeSquared < squareDistance)
                        {
                            continue;
                        }

                        elementsInRange.Add(element.value);
                    }
                }
            }
            else
            {
                float rangeSquared = range * range;
                foreach (KDTreeNode node in resultBuffer)
                {
                    foreach (Entry element in node.entries)
                    {
                        if ((element.position - position).sqrMagnitude <= rangeSquared)
                        {
                            elementsInRange.Add(element.value);
                        }
                    }
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
            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInBounds;
            }

            Stack<KDTreeNode> nodesToVisit = nodeBuffer ?? new Stack<KDTreeNode>();
            nodesToVisit.Clear();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out KDTreeNode currentNode))
            {
                if (currentNode.isTerminal)
                {
                    foreach (Entry element in currentNode.entries)
                    {
                        if (bounds.FastContains2D(element.position))
                        {
                            elementsInBounds.Add(element.value);
                        }
                    }

                    continue;
                }

                KDTreeNode leftNode = currentNode.left;
                if (0 < leftNode.entries.Length && bounds.FastIntersects2D(leftNode.boundary))
                {
                    nodesToVisit.Push(leftNode);
                }

                KDTreeNode rightNode = currentNode.right;
                if (0 < rightNode.entries.Length && bounds.FastIntersects2D(rightNode.boundary))
                {
                    nodesToVisit.Push(rightNode);
                }
            }

            return elementsInBounds;
        }

        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            using PooledResource<Stack<KDTreeNode>> nodeBufferResource =
                Buffers<KDTreeNode>.Stack.Get();
            Stack<KDTreeNode> nodeBuffer = nodeBufferResource.resource;
            using PooledResource<HashSet<T>> nearestNeighborBufferResource =
                Buffers<T>.HashSet.Get();
            HashSet<T> nearestNeighborBuffer = nearestNeighborBufferResource.resource;
            using PooledResource<List<Entry>> nearestNeighborsCacheResource =
                Buffers<Entry>.List.Get();
            List<Entry> nearestNeighborsCache = nearestNeighborsCacheResource.resource;
            GetApproximateNearestNeighbors(
                position,
                count,
                nearestNeighbors,
                nodeBuffer,
                nearestNeighborBuffer,
                nearestNeighborsCache
            );
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors,
            Stack<KDTreeNode> nodeBuffer,
            HashSet<T> nearestNeighborBuffer,
            List<Entry> nearestNeighborsCache
        )
        {
            nearestNeighbors.Clear();

            KDTreeNode current = _head;
            nodeBuffer ??= new Stack<KDTreeNode>();
            nodeBuffer.Clear();
            nodeBuffer.Push(_head);
            nearestNeighborBuffer ??= new HashSet<T>(count);
            nearestNeighborBuffer.Clear();
            nearestNeighborsCache ??= new List<Entry>(count);
            nearestNeighborsCache.Clear();

            while (!current.isTerminal)
            {
                KDTreeNode left = current.left;
                KDTreeNode right = current.right;
                if (
                    ((Vector2)left.boundary.center - position).sqrMagnitude
                    < ((Vector2)right.boundary.center - position).sqrMagnitude
                )
                {
                    nodeBuffer.Push(left);
                    current = left;
                    if (left.entries.Length <= count)
                    {
                        break;
                    }
                }
                else
                {
                    nodeBuffer.Push(right);
                    current = right;
                    if (right.entries.Length <= count)
                    {
                        break;
                    }
                }
            }

            while (
                nearestNeighborBuffer.Count < count && nodeBuffer.TryPop(out KDTreeNode selected)
            )
            {
                foreach (Entry element in selected.entries)
                {
                    if (nearestNeighborBuffer.Add(element.value))
                    {
                        nearestNeighborsCache.Add(element);
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
        }
    }
}
