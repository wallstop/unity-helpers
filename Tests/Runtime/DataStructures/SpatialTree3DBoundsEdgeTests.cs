namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using Bounds = UnityEngine.Bounds;
    using Vector3 = UnityEngine.Vector3;

    public sealed class SpatialTree3DBoundsEdgeTests
    {
        private static Vector3[] CreateGridPoints(int x, int y, int z)
        {
            Vector3[] points = new Vector3[x * y * z];
            int index = 0;
            for (int k = 0; k < z; ++k)
            {
                for (int j = 0; j < y; ++j)
                {
                    for (int i = 0; i < x; ++i)
                    {
                        points[index++] = new Vector3(i, j, k);
                    }
                }
            }
            return points;
        }

        [Test]
        public void FullBoundsOnGridConsistentAcrossTrees()
        {
            Vector3[] points = CreateGridPoints(100, 100, 100);

            var kdBalanced = new KdTree3D<Vector3>(points, p => p);
            var kdUnbalanced = new KdTree3D<Vector3>(points, p => p, balanced: false);
            var oct = new OctTree3D<Vector3>(points, p => p);

            Vector3 center = kdBalanced.Boundary.center;
            Vector3 size = new Vector3(99f, 99f, 99f);
            Bounds query = new Bounds(center, size);

            var buf = new List<Vector3>();
            kdBalanced.GetElementsInBounds(query, buf);
            int expected = buf.Count;

            kdUnbalanced.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);

            oct.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void EdgeAlignedBoundsConsistentAcrossTrees()
        {
            Vector3[] points = CreateGridPoints(10, 10, 10);

            var kd = new KdTree3D<Vector3>(points, p => p);
            var oct = new OctTree3D<Vector3>(points, p => p);

            Bounds query = new Bounds(new Vector3(4.5f, 4.5f, 4.5f), new Vector3(9f, 9f, 9f));

            var buf = new List<Vector3>();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;

            oct.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }

        [Test]
        public void UnitBoundsOnGridPointConsistentAcrossTrees()
        {
            Vector3[] points = CreateGridPoints(10, 10, 10);

            var kd = new KdTree3D<Vector3>(points, p => p);
            var oct = new OctTree3D<Vector3>(points, p => p);

            Bounds query = new Bounds(new Vector3(5f, 5f, 5f), Vector3.one);

            var buf = new List<Vector3>();
            kd.GetElementsInBounds(query, buf);
            int expected = buf.Count;

            oct.GetElementsInBounds(query, buf);
            Assert.AreEqual(expected, buf.Count);
        }
    }
}
