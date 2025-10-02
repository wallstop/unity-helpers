namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class QuadTree<T> : ISpatialTree<T>
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

        [Serializable]
        public sealed class QuadTreeNode
        {
            private static readonly List<Entry>[] Buffers = new List<Entry>[NumChildren];

            static QuadTreeNode()
            {
                for (int i = 0; i < Buffers.Length; ++i)
                {
                    Buffers[i] = new List<Entry>();
                }
            }

            public readonly Bounds boundary;
            internal readonly QuadTreeNode[] children;
            public readonly Entry[] elements;
            public readonly bool isTerminal;

            public QuadTreeNode(Entry[] elements, Bounds boundary, int bucketSize)
            {
                this.boundary = boundary;
                this.elements = elements;
                isTerminal = elements.Length <= bucketSize;
                if (isTerminal)
                {
                    children = Array.Empty<QuadTreeNode>();
                    return;
                }

                children = new QuadTreeNode[NumChildren];

                Vector3 quadrantSize = boundary.size / 2f;
                quadrantSize.z = 1;
                Vector3 halfQuadrantSize = quadrantSize / 2f;

                Vector3 boundaryCenter = boundary.center;
                Span<Bounds> quadrants = stackalloc Bounds[4];
                quadrants[0] = new Bounds(
                    new Vector3(
                        boundaryCenter.x - halfQuadrantSize.x,
                        boundaryCenter.y + halfQuadrantSize.y
                    ),
                    quadrantSize
                );
                quadrants[1] = new Bounds(
                    new Vector3(
                        boundaryCenter.x + halfQuadrantSize.x,
                        boundaryCenter.y + halfQuadrantSize.y
                    ),
                    quadrantSize
                );
                quadrants[2] = new Bounds(
                    new Vector3(
                        boundaryCenter.x + halfQuadrantSize.x,
                        boundaryCenter.y - halfQuadrantSize.y
                    ),
                    quadrantSize
                );
                quadrants[3] = new Bounds(
                    new Vector3(
                        boundaryCenter.x - halfQuadrantSize.x,
                        boundaryCenter.y - halfQuadrantSize.y
                    ),
                    quadrantSize
                );

                foreach (List<Entry> buffer in Buffers)
                {
                    buffer.Clear();
                }
                foreach (Entry element in elements)
                {
                    Vector2 position = element.position;
                    for (int i = 0; i < quadrants.Length; i++)
                    {
                        Bounds quadrant = quadrants[i];
                        if (quadrant.FastContains2D(position))
                        {
                            Buffers[i].Add(element);
                            break;
                        }
                    }
                }

                Entry[] entriesOne = Buffers[0].ToArray();
                Entry[] entriesTwo = Buffers[1].ToArray();
                Entry[] entriesThree = Buffers[2].ToArray();
                Entry[] entriesFour = Buffers[3].ToArray();

                children[0] = new QuadTreeNode(entriesOne, quadrants[0], bucketSize);
                children[1] = new QuadTreeNode(entriesTwo, quadrants[1], bucketSize);
                children[2] = new QuadTreeNode(entriesThree, quadrants[2], bucketSize);
                children[3] = new QuadTreeNode(entriesFour, quadrants[3], bucketSize);
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly QuadTreeNode _head;

        public QuadTree(
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
            bool anyPoints = false;
            Bounds bounds = new();
            Entry[] entries = new Entry[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                T element = elements[i];
                Vector2 position = elementTransformer(element);
                entries[i] = new Entry(element, position);
                if (!anyPoints)
                {
                    bounds = new Bounds(position, new Vector3(0, 0, 1f));
                }
                else
                {
                    bounds.Encapsulate(position);
                }

                anyPoints = true;
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
            _head = new QuadTreeNode(entries, _bounds, bucketSize);
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

            using PooledResource<Stack<QuadTreeNode>> nodesToVisitResource =
                Buffers<QuadTreeNode>.Stack.Get();
            Stack<QuadTreeNode> nodesToVisit = nodesToVisitResource.resource;
            nodesToVisit.Clear();
            nodesToVisit.Push(_head);

            using PooledResource<List<QuadTreeNode>> resultResource =
                Buffers<QuadTreeNode>.List.Get();
            List<QuadTreeNode> resultBuffer = resultResource.resource;
            resultBuffer.Clear();

            while (nodesToVisit.TryPop(out QuadTreeNode currentNode))
            {
                if (currentNode.isTerminal || bounds.Overlaps2D(currentNode.boundary))
                {
                    resultBuffer.Add(currentNode);
                    continue;
                }

                foreach (QuadTreeNode child in currentNode.children)
                {
                    if (child.elements.Length == 0)
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

            float rangeSquared = range * range;
            if (0 < minimumRange)
            {
                float minimumRangeSquared = minimumRange * minimumRange;
                foreach (QuadTreeNode node in resultBuffer)
                {
                    foreach (Entry element in node.elements)
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
                foreach (QuadTreeNode node in resultBuffer)
                {
                    foreach (Entry element in node.elements)
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
            using PooledResource<Stack<QuadTreeNode>> stackResource =
                Buffers<QuadTreeNode>.Stack.Get();
            return GetElementsInBounds(bounds, elementsInBounds, stackResource.resource);
        }

        public List<T> GetElementsInBounds(
            Bounds bounds,
            List<T> elementsInBounds,
            Stack<QuadTreeNode> nodeBuffer
        )
        {
            elementsInBounds.Clear();
            if (!bounds.FastIntersects2D(_bounds))
            {
                return elementsInBounds;
            }

            Stack<QuadTreeNode> nodesToVisit = nodeBuffer ?? new Stack<QuadTreeNode>();
            nodesToVisit.Clear();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out QuadTreeNode currentNode))
            {
                if (bounds.Overlaps2D(currentNode.boundary))
                {
                    foreach (Entry element in currentNode.elements)
                    {
                        elementsInBounds.Add(element.value);
                    }

                    continue;
                }

                if (currentNode.isTerminal)
                {
                    foreach (Entry element in currentNode.elements)
                    {
                        if (bounds.FastContains2D(element.position))
                        {
                            elementsInBounds.Add(element.value);
                        }
                    }

                    continue;
                }

                foreach (QuadTreeNode child in currentNode.children)
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

            return elementsInBounds;
        }

        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            using PooledResource<Stack<QuadTreeNode>> nodeBufferResource =
                Buffers<QuadTreeNode>.Stack.Get();
            Stack<QuadTreeNode> nodeBuffer = nodeBufferResource.resource;
            using PooledResource<List<QuadTreeNode>> childrenBufferResource =
                Buffers<QuadTreeNode>.List.Get();
            List<QuadTreeNode> childrenBuffer = childrenBufferResource.resource;
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
                childrenBuffer,
                nearestNeighborBuffer,
                nearestNeighborsCache
            );
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors,
            Stack<QuadTreeNode> nodeBuffer,
            List<QuadTreeNode> childrenBuffer,
            HashSet<T> nearestNeighborBuffer,
            List<Entry> nearestNeighborsCache
        )
        {
            QuadTreeNode current = _head;
            nodeBuffer ??= new Stack<QuadTreeNode>();
            nodeBuffer.Clear();
            nodeBuffer.Push(_head);
            childrenBuffer ??= new List<QuadTreeNode>(NumChildren);
            childrenBuffer.Clear();
            nearestNeighborBuffer ??= new HashSet<T>(count);
            nearestNeighborBuffer.Clear();
            nearestNeighborsCache ??= new List<Entry>(count);
            nearestNeighborsCache.Clear();

            Comparison<QuadTreeNode> comparison = Comparison;
            while (!current.isTerminal)
            {
                childrenBuffer.Clear();
                foreach (QuadTreeNode child in current.children)
                {
                    childrenBuffer.Add(child);
                }
                childrenBuffer.Sort(comparison);
                for (int i = childrenBuffer.Count - 1; 0 <= i; --i)
                {
                    nodeBuffer.Push(childrenBuffer[i]);
                }

                current = childrenBuffer[0];
                if (current.elements.Length <= count)
                {
                    break;
                }
            }

            while (
                nearestNeighborBuffer.Count < count && nodeBuffer.TryPop(out QuadTreeNode selected)
            )
            {
                foreach (Entry element in selected.elements)
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

            return;

            int Comparison(QuadTreeNode lhs, QuadTreeNode rhs) =>
                ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(
                    ((Vector2)rhs.boundary.center - position).sqrMagnitude
                );
        }
    }
}
