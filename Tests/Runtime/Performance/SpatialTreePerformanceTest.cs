namespace UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.DataStructure;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    public abstract class SpatialTreePerformanceTest<TTree>
        where TTree : ISpatialTree<Vector2>
    {
        protected abstract TTree CreateTree(IEnumerable<Vector2> points);

        [UnityTest]
        public IEnumerator Performance()
        {
            UnityEngine.Debug.Log("| Operation | Operations / Second |");
            UnityEngine.Debug.Log("| --------- | ------------------- |");

            Vector2[] points = new Vector2[1_000_000];
            float radius = 500;
            ParallelLoopResult result = Parallel.For(
                0,
                1_000_000,
                index =>
                {
                    // ReSharper disable once PossibleLossOfFraction
                    points[index] = new Vector2(index % 1_000, index / 1_000);
                }
            );

            while (!result.IsCompleted)
            {
                yield return null;
            }

            Stopwatch timer = Stopwatch.StartNew();
            TTree tree = CreateTree(points);
            timer.Stop();
            UnityEngine.Debug.Log(
                $"| Construction (1 million points) | {(int)Math.Floor(1 / timer.Elapsed.TotalSeconds)} ({timer.Elapsed.TotalSeconds} seconds total) |"
            );
            Vector2 center = tree.Boundary.center;
            int count = 0;
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            timer.Restart();
            do
            {
                int elementsInRange = tree.GetElementsInRange(center, radius).Count();
                Assert.AreEqual(785456, elementsInRange);
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
                int elementsInRange = tree.GetElementsInRange(center, radius).Count();
                Assert.AreEqual(196364, elementsInRange);
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
                int elementsInRange = tree.GetElementsInRange(center, radius).Count();
                Assert.AreEqual(49080, elementsInRange);
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
                int elementsInRange = tree.GetElementsInRange(center, radius).Count();
                Assert.AreEqual(4, elementsInRange);
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
        }
    }
}
