namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using DataStructure;
    using DataStructure.Adapters;
    using Helper;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    public static partial class UnityExtensions
    {
        /// <summary>
        /// Stops a Rigidbody2D by zeroing its velocity, angular velocity, and putting it to sleep.
        /// </summary>
        public static void Stop(this Rigidbody2D rigidBody)
        {
            if (rigidBody == null)
            {
                return;
            }

            rigidBody.velocity = Vector2.zero;
            rigidBody.angularVelocity = 0;
            rigidBody.Sleep();
        }

        /// <summary>
        /// Determines if a circle is fully contained within a Collider2D.
        /// </summary>
        public static bool IsCircleFullyContained(
            this Collider2D targetCollider,
            Vector2 center,
            float radius,
            int sampleCount = 16
        )
        {
            if (targetCollider == null)
            {
                throw new ArgumentNullException(nameof(targetCollider));
            }

            if (sampleCount <= 0)
            {
                sampleCount = 1;
            }

            float step = Mathf.PI * 2f / sampleCount;
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * step;
                Vector2 pointOnCircle =
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (!targetCollider.OverlapPoint(pointOnCircle))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inverts a PolygonCollider2D by making holes become solid and the outside become a hole.
        /// </summary>
        public static void Invert(this PolygonCollider2D col, Rect outerRect)
        {
            if (col == null)
            {
                throw new ArgumentNullException(nameof(col));
            }

            int originalCount = col.pathCount;
            if (originalCount == 0)
            {
                return;
            }

            using PooledResource<Vector2[][]> originalBuffer = WallstopArrayPool<Vector2[]>.Get(
                originalCount,
                out Vector2[][] originals
            );
            using PooledResource<List<PooledResource<Vector2[]>>> pathBuffer = Buffers<
                PooledResource<Vector2[]>
            >.List.Get(out List<PooledResource<Vector2[]>> paths);

            for (int i = 0; i < originalCount; i++)
            {
                Vector2[] path = col.GetPath(i);
                PooledResource<Vector2[]> buffer = WallstopArrayPool<Vector2>.Get(
                    path.Length,
                    out Vector2[] points
                );
                paths.Add(buffer);
                Array.Copy(path, points, path.Length);
                originals[i] = points;
            }

            Vector2[] outerPath =
            {
                new(outerRect.xMin, outerRect.yMin),
                new(outerRect.xMin, outerRect.yMax),
                new(outerRect.xMax, outerRect.yMax),
                new(outerRect.xMax, outerRect.yMin),
            };

            col.pathCount = originalCount + 1;
            col.SetPath(0, outerPath);

            for (int i = 0; i < originalCount; ++i)
            {
                Vector2[] hole = originals[i];
                Array.Reverse(hole);
                col.SetPath(i + 1, hole);
            }

            foreach (PooledResource<Vector2[]> path in paths)
            {
                path.Dispose();
            }
        }
    }
}
