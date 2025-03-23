namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Extension;
    using UnityEngine;
    using Utils;

    [Serializable]
    public sealed class KDTree<T> : ISpatialTree<T>
    {
        public delegate float Axis<in V>(V element);

        [Serializable]
        public sealed class KDTreeNode<V>
        {
            public readonly Bounds boundary;
            public readonly KDTreeNode<V> left;
            public readonly KDTreeNode<V> right;
            public readonly V[] elements;
            public readonly bool isTerminal;

            public KDTreeNode(
                List<V> elements,
                Func<V, Vector2> elementTransformer,
                int bucketSize,
                bool isXAxis,
                bool balanced
            )
            {
                boundary = elements.Select(elementTransformer).GetBounds() ?? new Bounds();
                this.elements = elements.ToArray();
                isTerminal = elements.Count <= bucketSize;
                if (isTerminal)
                {
                    return;
                }

                if (balanced)
                {
                    Axis<V> axisFunction = isXAxis
                        ? element => elementTransformer(element).x
                        : element => elementTransformer(element).y;

                    int Comparison(V lhs, V rhs)
                    {
                        return axisFunction(lhs).CompareTo(axisFunction(rhs));
                    }
                    elements.Sort(Comparison);

                    int cutoff = elements.Count / 2;
                    left = new KDTreeNode<V>(
                        elements.Take(cutoff).ToList(),
                        elementTransformer,
                        bucketSize,
                        !isXAxis,
                        true
                    );
                    right = new KDTreeNode<V>(
                        elements.Skip(cutoff).ToList(),
                        elementTransformer,
                        bucketSize,
                        !isXAxis,
                        true
                    );
                }
                else
                {
                    Vector2 cutoff = boundary.center;
                    if (isXAxis)
                    {
                        left = new KDTreeNode<V>(
                            elements
                                .Where(element => elementTransformer(element).x <= cutoff.x)
                                .ToList(),
                            elementTransformer,
                            bucketSize,
                            false,
                            false
                        );
                        right = new KDTreeNode<V>(
                            elements
                                .Where(element => cutoff.x < elementTransformer(element).x)
                                .ToList(),
                            elementTransformer,
                            bucketSize,
                            false,
                            false
                        );
                    }
                    else
                    {
                        left = new KDTreeNode<V>(
                            elements
                                .Where(element => elementTransformer(element).y <= cutoff.y)
                                .ToList(),
                            elementTransformer,
                            bucketSize,
                            true,
                            false
                        );
                        right = new KDTreeNode<V>(
                            elements
                                .Where(element => cutoff.y < elementTransformer(element).y)
                                .ToList(),
                            elementTransformer,
                            bucketSize,
                            true,
                            false
                        );
                    }
                }
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;
        public Func<T, Vector2> ElementTransformer => _elementTransformer;

        private readonly Bounds _bounds;
        private readonly Func<T, Vector2> _elementTransformer;
        private readonly KDTreeNode<T> _head;

        public KDTree(
            IEnumerable<T> points,
            Func<T, Vector2> elementTransformer,
            int bucketSize = DefaultBucketSize,
            bool balanced = true
        )
        {
            _elementTransformer =
                elementTransformer ?? throw new ArgumentNullException(nameof(elementTransformer));
            elements =
                points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));
            _bounds = elements.Select(elementTransformer).GetBounds() ?? new Bounds();
            _head = new KDTreeNode<T>(
                elements.ToList(),
                elementTransformer,
                bucketSize: bucketSize,
                isXAxis: true,
                balanced: balanced
            );
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds)
        {
            Stack<KDTreeNode<T>> buffer = Buffers<KDTreeNode<T>>.Stack;
            return GetElementsInBounds(bounds, buffer);
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds, Stack<KDTreeNode<T>> nodeBuffer)
        {
            if (!bounds.FastIntersects2D(_bounds))
            {
                yield break;
            }

            Stack<KDTreeNode<T>> nodesToVisit = nodeBuffer ?? new Stack<KDTreeNode<T>>();
            nodesToVisit.Clear();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out KDTreeNode<T> currentNode))
            {
                if (currentNode.isTerminal)
                {
                    foreach (T element in currentNode.elements)
                    {
                        if (bounds.FastContains2D(_elementTransformer(element)))
                        {
                            yield return element;
                        }
                    }

                    continue;
                }

                if (bounds.Overlaps2D(currentNode.boundary))
                {
                    foreach (T element in currentNode.elements)
                    {
                        yield return element;
                    }

                    continue;
                }

                KDTreeNode<T> leftNode = currentNode.left;
                if (0 < leftNode.elements.Length && bounds.FastIntersects2D(leftNode.boundary))
                {
                    nodesToVisit.Push(leftNode);
                }

                KDTreeNode<T> rightNode = currentNode.right;
                if (0 < rightNode.elements.Length && bounds.FastIntersects2D(rightNode.boundary))
                {
                    nodesToVisit.Push(rightNode);
                }
            }
        }

        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors
        )
        {
            Stack<KDTreeNode<T>> nodeBuffer = Buffers<KDTreeNode<T>>.Stack;
            HashSet<T> nearestNeighborBuffer = Buffers<T>.HashSet;
            GetApproximateNearestNeighbors(
                position,
                count,
                nearestNeighbors,
                nodeBuffer,
                nearestNeighborBuffer
            );
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(
            Vector2 position,
            int count,
            List<T> nearestNeighbors,
            Stack<KDTreeNode<T>> nodeBuffer,
            HashSet<T> nearestNeighborBuffer
        )
        {
            nearestNeighbors.Clear();

            KDTreeNode<T> current = _head;
            Stack<KDTreeNode<T>> stack = nodeBuffer ?? new Stack<KDTreeNode<T>>();
            stack.Clear();
            stack.Push(_head);
            HashSet<T> nearestNeighborsSet = nearestNeighborBuffer ?? new HashSet<T>(count);
            nearestNeighborsSet.Clear();

            while (!current.isTerminal)
            {
                KDTreeNode<T> left = current.left;
                KDTreeNode<T> right = current.right;
                if (
                    ((Vector2)left.boundary.center - position).sqrMagnitude
                    < ((Vector2)right.boundary.center - position).sqrMagnitude
                )
                {
                    stack.Push(left);
                    current = left;
                    if (left.elements.Length <= count)
                    {
                        break;
                    }
                }
                else
                {
                    stack.Push(right);
                    current = right;
                    if (right.elements.Length <= count)
                    {
                        break;
                    }
                }
            }

            while (nearestNeighborsSet.Count < count && stack.TryPop(out KDTreeNode<T> selected))
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
                    (_elementTransformer(lhs) - localPosition).sqrMagnitude.CompareTo(
                        (_elementTransformer(rhs) - localPosition).sqrMagnitude
                    );
            }
        }
    }
}
