namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class RTreePerformanceTests
    {
        private const float PointBoundsSize = 0.001f;

        [UnityTest]
        [Timeout(0)]
        public IEnumerator Performance()
        {
            UnityEngine.Debug.Log("| Operation | Operations / Second |");
            UnityEngine.Debug.Log("| --------- | ------------------- |");

            Vector2[] points = new Vector2[1_000_000];
            Parallel.For(
                0,
                1_000,
                y =>
                {
                    for (int x = 0; x < 1_000; ++x)
                    {
                        int index = y * 1_000 + x;
                        points[index] = new Vector2(x, y);
                    }
                }
            );

            Stopwatch timer = Stopwatch.StartNew();
            RTree<Vector2> tree = new(points, CreateBounds);
            timer.Stop();
            UnityEngine.Debug.Log(
                $"| Construction (1 million points) | {(int)Math.Floor(1 / timer.Elapsed.TotalSeconds)} ({timer.Elapsed.TotalSeconds} seconds total) |"
            );

            Vector2 center = (Vector2)tree.Boundary.center;
            float radius = 500f;
            TimeSpan timeout = TimeSpan.FromSeconds(1);

            List<Vector2> elementsInRange = new();
            int count = 0;
            timer.Restart();
            do
            {
                tree.GetElementsInRange(center, radius, elementsInRange);
                Assert.AreEqual(785456, elementsInRange.Count);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| Elements In Range - Full | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            radius /= 2f;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetElementsInRange(center, radius, elementsInRange);
                Assert.AreEqual(196364, elementsInRange.Count);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| Elements In Range - Half | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            radius /= 2f;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetElementsInRange(center, radius, elementsInRange);
                Assert.AreEqual(49080, elementsInRange.Count);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| Elements In Range - Quarter | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            radius = 1f;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetElementsInRange(center, radius, elementsInRange);
                Assert.AreEqual(4, elementsInRange.Count);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| Elements In Range - 1 Range | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            int nearestNeighborCount = 500;
            List<Vector2> nearestNeighbors = new();
            count = 0;
            timer.Restart();
            do
            {
                tree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| ANN - 500 | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            nearestNeighborCount = 100;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| ANN - 100 | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            nearestNeighborCount = 10;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log(
                $"| ANN - 10 | {(int)Math.Floor(count / timeout.TotalSeconds)} |"
            );

            nearestNeighborCount = 1;
            count = 0;
            timer.Restart();
            do
            {
                tree.GetApproximateNearestNeighbors(center, nearestNeighborCount, nearestNeighbors);
                Assert.IsTrue(nearestNeighbors.Count <= nearestNeighborCount);
                ++count;
            } while (timer.Elapsed < timeout);
            UnityEngine.Debug.Log($"| ANN - 1 | {(int)Math.Floor(count / timeout.TotalSeconds)} |");

            yield break;
        }

        private static Bounds CreateBounds(Vector2 point)
        {
            return new Bounds(
                new Vector3(point.x, point.y, 0f),
                new Vector3(PointBoundsSize, PointBoundsSize, 1f)
            );
        }
    }
}
