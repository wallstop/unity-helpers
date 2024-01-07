namespace UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Core.DataStructure;
    using Core.Extension;
    using Core.Helper;
    using Core.Random;
    using NUnit.Framework;
    using Vector2 = UnityEngine.Vector2;

    public sealed class QuadTreeTests
    {
        private IRandom Random => PcgRandom.Instance;

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
                }
                while (!points.Add(point));
            }

            QuadTree<Vector2> quadTree = new(points, _ => _);

            List<Vector2> pointsInRange = quadTree.GetElementsInRange(center, radius).ToList();
            Assert.IsTrue(points.SetEquals(pointsInRange), "Found {0} points in range, expected {1}.", pointsInRange.Count, points.Count);
            // Translate by a unit-square - there should be no points in this range
            Vector2 offset = center;
            offset.x -= radius * 2;
            offset.y -= radius * 2;

            pointsInRange = quadTree.GetElementsInRange(offset, radius).ToList();
            Assert.AreEqual(
                0, pointsInRange.Count, "Found {0} points within {1} range of {2} (original center {3})",
                pointsInRange.Count, radius, offset, center);
        }

        [Test]
        public void SimplePointOutsideRange()
        {
            Vector2 point = new(Random.NextFloat(-100, 100), Random.NextFloat(-100, 100));

            Vector2 direction = Helpers.GetRandomPointInCircle(Vector2.zero, 1f).normalized;
            float range = Random.NextFloat(25, 1_000);
            Vector2 testPoint = point + (direction * range);
            List<Vector2> points = new(1) { testPoint };

            QuadTree<Vector2> quadTree = new(points, _ => _);
            List<Vector2> pointsInRange = quadTree.GetElementsInRange(point, range * 0.99f).ToList();
            Assert.AreEqual(0, pointsInRange.Count);
            pointsInRange = quadTree.GetElementsInRange(point, range).ToList();
            Assert.AreEqual(1, pointsInRange.Count);
            Assert.AreEqual(testPoint, pointsInRange[0]);
        }

        [Test]
        public void Performance()
        {
            List<Vector2> points = new();
            float radius = 500;
            for (int x = 0; x < 1_000; ++x)
            {
                for (int y = 0; y < 1_000; ++y)
                {
                    Vector2 point = new(x, y);
                    points.Add(point);
                }
            }

            QuadTree<Vector2> quadTree = new(points, _ => _);
            Vector2 center = quadTree.Bounds.center;
            int count = 0;
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = quadTree.GetElementsInRange(center, radius).Count();
                ++count;
            }
            while (timer.Elapsed < timeout);

            UnityEngine.Debug.Log("| Operation | Operations / Second |");
            UnityEngine.Debug.Log("| ----- | ------------------- |");
            UnityEngine.Debug.Log($"| Elements In Range - Full | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            radius /= 2f;
            count = 0;
            timer.Restart();
            do
            {
                _ = quadTree.GetElementsInRange(center, radius).Count();
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| Elements In Range - Half | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            radius /= 2f;
            count = 0;
            timer.Restart();
            do
            {
                _ = quadTree.GetElementsInRange(center, radius).Count();
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| Elements In Range - Quarter | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            radius = 1f;
            count = 0;
            timer.Restart();
            do
            {
                _ = quadTree.GetElementsInRange(center, radius).Count();
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| Elements In Range - 1 Range | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            int nearestNeighborCount = 500;
            List<Vector2> nearestNeighbors = new();
            count = 0;
            timer.Restart();
            do
            {
                quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| ANN - 500 | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            nearestNeighborCount = 100;
            count = 0;
            timer.Restart();
            do
            {
                quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| ANN - 100 | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            nearestNeighborCount = 10;
            count = 0;
            timer.Restart();
            do
            {
                quadTree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            }
            while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| ANN - 10 | {(int)Math.Floor(count / timeout.TotalSeconds)} |");
        }
    }
}
