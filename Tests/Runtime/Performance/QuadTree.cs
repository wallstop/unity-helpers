namespace UnityHelpers.Tests.Performance
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System;
    using System.Linq;
    using UnityEngine;
    using Core.DataStructure;

    public sealed class QuadTree
    {
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
