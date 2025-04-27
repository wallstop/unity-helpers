namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public interface ISpatialTree<T>
    {
        Bounds Boundary { get; }
        Func<T, Vector2> ElementTransformer { get; }

        IEnumerable<T> GetElementsInRange(Vector2 position, float range, float minimumRange = 0f)
        {
            Circle area = new(position, range);
            Func<T, Vector2> elementTransformer = ElementTransformer;
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
                        Vector2 elementPosition = elementTransformer(element);
                        if (!area.Contains(elementPosition))
                        {
                            return false;
                        }

                        return !minimumArea.Contains(elementPosition);
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
                    Vector2 elementPosition = elementTransformer(element);
                    if (!area.Contains(elementPosition))
                    {
                        return false;
                    }

                    return true;
                });
        }

        IEnumerable<T> GetElementsInBounds(Bounds bounds);

        void GetApproximateNearestNeighbors(Vector2 position, int count, List<T> nearestNeighbors);
    }
}
