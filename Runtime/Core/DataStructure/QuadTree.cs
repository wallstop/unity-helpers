namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extension;
    using UnityEngine;

    public sealed class QuadTree<T>
    {
        private sealed class QuadTreeNode<V>
        {
            private const int NumChildren = 4;

            public readonly Bounds boundary;
            public readonly IReadOnlyList<QuadTreeNode<V>> children;
            public readonly IReadOnlyList<V> elements;
            public readonly bool isTerminal;

            public QuadTreeNode(IReadOnlyList<V> elements, Func<V, Vector2> elementTransformer, Bounds boundary,
                int bucketSize)
            {
                this.boundary = boundary;
                isTerminal = elements.Count <= bucketSize;
                List<QuadTreeNode<V>> childrenList = new(isTerminal ? 0 : NumChildren);
                children = childrenList;
                this.elements = elements;
                if (isTerminal)
                {
                    return;
                }

                Vector3 quadrantSize = boundary.size / 2f;
                Vector2 halfQuadrantSize = quadrantSize / 2f;

                Bounds[] quadrants =
                {
                    new Bounds(new Vector3(boundary.center.x - halfQuadrantSize.x, boundary.center.y + halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x + halfQuadrantSize.x, boundary.center.y + halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x + halfQuadrantSize.x, boundary.center.y - halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x - halfQuadrantSize.x, boundary.center.y - halfQuadrantSize.y, boundary.center.z), quadrantSize),
                };

                foreach (Bounds quadrant in quadrants)
                {
                    List<V> pointsInRange = elements
                        .Where(element => quadrant.FastContains2D(elementTransformer(element))).ToList();
                    QuadTreeNode<V> child = new(pointsInRange, elementTransformer, quadrant, bucketSize);
                    childrenList.Add(child);
                }
            }
        }

        private const int DefaultBucketSize = 12;

        public readonly IReadOnlyList<T> elements;

        private readonly Bounds _bounds;
        private readonly Func<T, Vector2> _elementTransformer;
        private readonly QuadTreeNode<T> _head;

        public QuadTree(IEnumerable<T> points, Func<T, Vector2> elementTransformer, Bounds boundary,
            int bucketSize = DefaultBucketSize)
        {
            _elementTransformer = elementTransformer;
            _bounds = boundary;
            elements = points.ToList();
            _head = new QuadTreeNode<T>(elements, elementTransformer, boundary, bucketSize);
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

            Queue<QuadTreeNode<T>> nodesToVisit = new();
            nodesToVisit.Enqueue(_head);

            do
            {
                QuadTreeNode<T> currentNode = nodesToVisit.Dequeue();
                if (!bounds.FastIntersects2D(currentNode.boundary))
                {
                    continue;
                }
                
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

                foreach (QuadTreeNode<T> child in currentNode.children)
                {
                    nodesToVisit.Enqueue(child);
                }
            }
            while (0 < nodesToVisit.Count);
        }

        // Heavily adapted http://homepage.divms.uiowa.edu/%7Ekvaradar/sp2012/daa/ann.pdf
        public void GetApproximateNearestNeighbors(Vector2 position, int count, List<T> nearestNeighbors)
        {
            nearestNeighbors.Clear();

            QuadTreeNode<T> current = _head;
            Stack<QuadTreeNode<T>> stack = new();
            stack.Push(_head);
            List<QuadTreeNode<T>> childrenCopy = new(4);
            HashSet<T> nearestNeighborsSet = new(count);

            int Comparison(QuadTreeNode<T> lhs, QuadTreeNode<T> rhs) => ((Vector2)lhs.boundary.center - position).sqrMagnitude.CompareTo(((Vector2)rhs.boundary.center - position).sqrMagnitude);
            
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
                if (current.elements.Count <= count)
                {
                    break;
                }
            }

            while (nearestNeighborsSet.Count < count && 0 < stack.Count)
            {
                QuadTreeNode<T> selected = stack.Pop();
                nearestNeighborsSet.UnionWith(selected.elements);
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
