﻿namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Extension;
    using UnityEngine;

    [Serializable]
    public sealed class QuadTree<T> : ISpatialTree<T>
    {
        private const int NumChildren = 4;

        [Serializable]
        private sealed class QuadTreeNode<V>
        {
            private static readonly List<V> Buffer = new();

            public readonly Bounds boundary;
            public readonly QuadTreeNode<V>[] children;
            public readonly V[] elements;
            public readonly bool isTerminal;

            public QuadTreeNode(V[] elements, Func<V, Vector2> elementTransformer, Bounds boundary,
                int bucketSize)
            {
                this.boundary = boundary;
                this.elements = elements;
                isTerminal = elements.Length <= bucketSize;
                if (isTerminal)
                {
                    children = Array.Empty<QuadTreeNode<V>>();
                    return;
                }
                children = new QuadTreeNode<V>[NumChildren];
                
                Vector3 quadrantSize = boundary.size / 2f;
                Vector2 halfQuadrantSize = quadrantSize / 2f;

                Bounds[] quadrants =
                {
                    new Bounds(new Vector3(boundary.center.x - halfQuadrantSize.x, boundary.center.y + halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x + halfQuadrantSize.x, boundary.center.y + halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x + halfQuadrantSize.x, boundary.center.y - halfQuadrantSize.y, boundary.center.z), quadrantSize),
                    new Bounds(new Vector3(boundary.center.x - halfQuadrantSize.x, boundary.center.y - halfQuadrantSize.y, boundary.center.z), quadrantSize),
                };

                for (int i = 0; i < quadrants.Length; ++i)
                {
                    Bounds quadrant = quadrants[i];
                    Buffer.Clear();
                    foreach (V element in elements)
                    {
                        if (quadrant.FastContains2D(elementTransformer(element)))
                        {
                            Buffer.Add(element);
                        }
                    }

                    children[i] = new QuadTreeNode<V>(Buffer.ToArray(), elementTransformer, quadrant, bucketSize);
                }
            }
        }

        public const int DefaultBucketSize = 12;

        public readonly ImmutableArray<T> elements;
        public Bounds Boundary => _bounds;
        public Func<T, Vector2> ElementTransformer => _elementTransformer;

        private readonly Bounds _bounds;
        private readonly Func<T, Vector2> _elementTransformer;
        private readonly QuadTreeNode<T> _head;

        public QuadTree(IEnumerable<T> points, Func<T, Vector2> elementTransformer, Bounds? boundary = null,
            int bucketSize = DefaultBucketSize)
        {
            _elementTransformer = elementTransformer ?? throw new ArgumentNullException(nameof(elementTransformer));
            elements = points?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(points));
            _bounds = boundary ?? elements.Select(elementTransformer).GetBounds() ?? new Bounds();
            _head = new QuadTreeNode<T>(elements.ToArray(), elementTransformer, _bounds, bucketSize);
        }

        public IEnumerable<T> GetElementsInBounds(Bounds bounds)
        {
            if (!bounds.FastIntersects2D(_bounds))
            {
                yield break;
            }

            Stack<QuadTreeNode<T>> nodesToVisit = new();
            nodesToVisit.Push(_head);

            while (nodesToVisit.TryPop(out QuadTreeNode<T> currentNode))
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

                foreach (QuadTreeNode<T> child in currentNode.children)
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

            QuadTreeNode<T> current = _head;
            Stack<QuadTreeNode<T>> stack = new();
            stack.Push(_head);
            List<QuadTreeNode<T>> childrenCopy = new(NumChildren);
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
                if (current.elements.Length <= count)
                {
                    break;
                }
            }

            while (nearestNeighborsSet.Count < count && stack.TryPop(out QuadTreeNode<T> selected))
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
