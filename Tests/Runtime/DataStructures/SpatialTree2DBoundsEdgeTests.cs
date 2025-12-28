// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using Bounds = UnityEngine.Bounds;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public sealed class SpatialTree2DBoundsEdgeTests
    {
        private static Vector2[] CreateGridPoints(int width, int height)
        {
            Vector2[] points = new Vector2[width * height];
            int index = 0;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    points[index++] = new Vector2(x, y);
                }
            }
            return points;
        }

        [Test]
        public void FullBoundsOnGridConsistentAcrossTrees()
        {
            Vector2[] points = CreateGridPoints(100, 100);

            KdTree2D<Vector2> kdBalanced = new(points, p => p);
            KdTree2D<Vector2> kdUnbalanced = new(points, p => p, balanced: false);
            QuadTree2D<Vector2> quad = new(points, p => p);

            Vector3 center3 = kdBalanced.Boundary.center;
            Vector2 center = new(center3.x, center3.y);
            Vector3 size = new(99f, 99f, 1f);
            Bounds query = new(new Vector3(center.x, center.y, 0f), size);

            List<Vector2> buf = new();
            kdBalanced.GetElementsInBounds(query, buf);
            int expected = buf.Count;

            kdUnbalanced.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);

            quad.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void EdgeAlignedBoundsConsistentAcrossTrees()
        {
            Vector2[] points = CreateGridPoints(10, 10);

            KdTree2D<Vector2> kd = new(points, p => p);
            QuadTree2D<Vector2> quad = new(points, p => p);

            Bounds query = new(new Vector3(4.5f, 4.5f, 0f), new Vector3(9f, 9f, 1f));

            List<Vector2> buf = new();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;
            Assert.AreEqual(100, expected);

            quad.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void UnitBoundsOnGridPointConsistentAcrossTrees()
        {
            Vector2[] points = CreateGridPoints(10, 10);

            KdTree2D<Vector2> kd = new(points, p => p);
            QuadTree2D<Vector2> quad = new(points, p => p);

            Bounds query = new(new Vector3(5f, 5f, 0f), new Vector3(1f, 1f, 1f));

            List<Vector2> buf = new();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;
            Assert.AreEqual(1, expected);

            quad.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void TouchingMaxEdgeConsistentAcrossTrees()
        {
            Vector2[] points = CreateGridPoints(10, 10);

            KdTree2D<Vector2> kd = new(points, p => p);
            QuadTree2D<Vector2> quad = new(points, p => p);

            Bounds query = new(new Vector3(4.5f, 9f, 0f), new Vector3(9f, 1f, 1f));

            List<Vector2> buf = new();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;
            Assert.AreEqual(10, expected);

            quad.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void TouchingMinEdgeConsistentAcrossTrees()
        {
            Vector2[] points = CreateGridPoints(10, 10);

            KdTree2D<Vector2> kd = new(points, p => p);
            QuadTree2D<Vector2> quad = new(points, p => p);

            Bounds query = new(new Vector3(0f, 4.5f, 0f), new Vector3(1f, 9f, 1f));

            List<Vector2> buf = new();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;
            Assert.AreEqual(10, expected);

            quad.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }
    }
}
