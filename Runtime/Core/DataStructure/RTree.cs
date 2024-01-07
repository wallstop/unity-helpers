namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Extension;
    using UnityEngine;

    [Serializable]
    public sealed class RTree<T> : ISpatialTree<T>
    {
        [Serializable]
        private sealed class RTreeNode<V>
        {
            public readonly Bounds boundary;
            public readonly RTreeNode<V>[] children;
            public readonly V[] elements;
            public readonly bool isTerminal;

            public RTreeNode(List<V> elements, Func<V, Vector2> elementTransformer, int bucketSize, int branchFactor)
            {
                boundary = elements.Select(elementTransformer).GetBounds() ?? new Bounds();
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

                int XAxis(V lhs, V rhs)
                {
                    return elementTransformer(lhs).x.CompareTo(elementTransformer(rhs).x);
                }

                int YAxis(V lhs, V rhs)
                {
                    return elementTransformer(lhs).y.CompareTo(elementTransformer(rhs).y);
                }

                elements.Sort(XAxis);
                foreach (List<V> xSlice in elements.Partition(rectanglesPerPagePerAxis).Select(enumerable => enumerable as List<V> ?? enumerable.ToList()))
                {
                    xSlice.Sort(YAxis);
                    foreach (List<V> ySlice in xSlice.Partition(intTargetSize).Select(enumerable => enumerable as List<V> ?? enumerable.ToList()))
                    {
                        RTreeNode<V> node = new(ySlice, elementTransformer, bucketSize, branchFactor);
                        tempChildren.Add(node);
                    }
                }

                children = tempChildren.ToArray();
            }
        }

        public const int DefaultBucketSize = 10;
        public const int DefaultBranchFactor = 4;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;

        private readonly Bounds _bounds;
        private readonly Func<T, Vector2> _elementTransformer;
        private readonly RTreeNode<T> _head;

        public RTree(
            IEnumerable<T> points, Func<T, Vector2> elementTransformer, int bucketSize = DefaultBucketSize,
            int branchFactor = DefaultBranchFactor)
        {
            _elementTransformer = elementTransformer ?? throw new ArgumentNullException(nameof(elementTransformer));
            elements = points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));
            _bounds = elements.Select(elementTransformer).GetBounds() ?? new Bounds();
            _head = new RTreeNode<T>(elements.ToList(), elementTransformer, bucketSize, branchFactor);
        }

        public IEnumerable<T> GetElementsInRange(Vector2 position, float range, float minimumRange = 0f)
        {
            Circle area = new(position, range);
            Circle minimumArea = new(position, minimumRange);
            return GetElementsInBounds(new Bounds(new Vector3(position.x, position.y, 0f),
                    new Vector3(range * 2f, range * 2f, 1f)))
                .Where(element =>
                {
                    Vector2 elementPosition = _elementTransformer(element);
                    if (!area.Contains(elementPosition))
                    {
                        return false;
                    }

                    if (minimumRange != 0f)
                    {
                        return !minimumArea.Contains(elementPosition);
                    }

                    return true;
                });
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds)
        {
            if (!bounds.FastIntersects2D(_bounds))
            {
                yield break;
            }

            Stack<RTreeNode<T>> nodesToVisit = new();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out RTreeNode<T> currentNode))
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

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(Vector2 position, int count, List<T> nearestNeighbors)
        {
            nearestNeighbors.Clear();

            RTreeNode<T> current = _head;
            Stack<RTreeNode<T>> stack = new();
            stack.Push(_head);
            List<RTreeNode<T>> childrenCopy = new();
            HashSet<T> nearestNeighborsSet = new(count);

            int Comparison(RTreeNode<T> lhs, RTreeNode<T> rhs) => ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(((Vector2)rhs.boundary.center - position).sqrMagnitude);

            while (!current.isTerminal)
            {
                childrenCopy.Clear();
                childrenCopy.AddRange(current.children);
                childrenCopy.Sort(Comparison);
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

            nearestNeighbors.AddRange(nearestNeighborsSet);
            if (count < nearestNeighbors.Count)
            {
                int NearestComparison(T lhs, T rhs) => (_elementTransformer(lhs) - position).sqrMagnitude.CompareTo((_elementTransformer(rhs) - position).sqrMagnitude);
                nearestNeighbors.Sort(NearestComparison);
                nearestNeighbors.RemoveRange(count, nearestNeighbors.Count - count);
            }
        }
    }
}
