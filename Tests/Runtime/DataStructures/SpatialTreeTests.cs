namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataStructure;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;
    using Vector2 = UnityEngine.Vector2;

    public abstract class SpatialTreeTests<TTree>
        where TTree : ISpatialTree<Vector2>
    {
        private IRandom Random => PRNG.Instance;

        protected abstract TTree CreateTree(IEnumerable<Vector2> points);

        [Test]
        public void SimpleWithinCircle()
        {
            Vector2 center = new(Random.NextFloat(-100, 100), Random.NextFloat(-100, 100));
            float radius = Random.NextFloat(5, 25f);

            const int numPoints = 1_000;
            HashSet<Vector2> points = new(numPoints);
            for (int i = 0; i < numPoints; ++i)
            {
                Vector2 point;
                do
                {
                    point = Helpers.GetRandomPointInCircle(center, radius);
                } while (!points.Add(point));
            }

            TTree quadTree = CreateTree(points);

            List<Vector2> pointsInRange = quadTree.GetElementsInRange(center, radius).ToList();
            Assert.IsTrue(
                points.SetEquals(pointsInRange),
                "Found {0} points in range, expected {1}.",
                pointsInRange.Count,
                points.Count
            );
            // Translate by a unit-square - there should be no points in this range
            Vector2 offset = center;
            offset.x -= radius * 2;
            offset.y -= radius * 2;

            pointsInRange = quadTree.GetElementsInRange(offset, radius).ToList();
            Assert.AreEqual(
                0,
                pointsInRange.Count,
                "Found {0} points within {1} range of {2} (original center {3})",
                pointsInRange.Count,
                radius,
                offset,
                center
            );
        }

        [Test]
        public void SimplePointOutsideRange()
        {
            Vector2 point = new(Random.NextFloat(-100, 100), Random.NextFloat(-100, 100));

            Vector2 direction = Helpers.GetRandomPointInCircle(Vector2.zero, 1f).normalized;
            float range = Random.NextFloat(25, 1_000);
            Vector2 testPoint = point + (direction * range);
            List<Vector2> points = new(1) { testPoint };

            TTree quadTree = CreateTree(points);
            List<Vector2> pointsInRange = quadTree
                .GetElementsInRange(point, range * 0.99f)
                .ToList();
            Assert.AreEqual(0, pointsInRange.Count);
            pointsInRange = quadTree.GetElementsInRange(point, range * 1.01f).ToList();
            Assert.AreEqual(
                1,
                pointsInRange.Count,
                "Failed to find point {0} from test point {1} with {2:0.00} range.",
                point,
                testPoint,
                range
            );
            Assert.AreEqual(testPoint, pointsInRange[0]);
        }

        [Test]
        public void SimpleANN()
        {
            List<Vector2> points = new();
            for (int x = 0; x < 100; ++x)
            {
                for (int y = 0; y < 100; ++y)
                {
                    Vector2 point = new(x, y);
                    points.Add(point);
                }
            }

            TTree quadTree = CreateTree(points);
            Vector2 center = quadTree.Boundary.center;

            List<Vector2> nearestNeighbors = new();
            int nearestNeighborCount = 1;
            quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
            Assert.AreEqual(nearestNeighborCount, nearestNeighbors.Count);
            Assert.IsTrue(nearestNeighbors.All(neighbor => (neighbor - center).magnitude <= 2f));

            nearestNeighborCount = 4;
            quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
            Assert.AreEqual(nearestNeighborCount, nearestNeighbors.Count);
            Assert.IsTrue(nearestNeighbors.All(neighbor => (neighbor - center).magnitude <= 2.2f));

            nearestNeighborCount = 16;
            quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
            Assert.AreEqual(nearestNeighborCount, nearestNeighbors.Count);
            Assert.IsTrue(
                nearestNeighbors.All(neighbor => (neighbor - center).magnitude <= 5.6f),
                "Max: {0}",
                nearestNeighbors.Select(neighbor => (neighbor - center).magnitude).Max()
            );

            center = new Vector2(-100, -100);

            quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
            Assert.AreEqual(nearestNeighborCount, nearestNeighbors.Count);
        }
    }
}
