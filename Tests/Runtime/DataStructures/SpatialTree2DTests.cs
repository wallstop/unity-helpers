// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;
    using Bounds = UnityEngine.Bounds;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public abstract class SpatialTree2DTests<TTree>
        where TTree : ISpatialTree2D<Vector2>
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

            List<Vector2> pointsInRange = new();

            quadTree.GetElementsInRange(center, radius, pointsInRange);

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

            quadTree.GetElementsInRange(offset, radius, pointsInRange);

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

            List<Vector2> pointsInRange = new();

            quadTree.GetElementsInRange(point, range * 0.99f, pointsInRange).ToList();

            Assert.AreEqual(0, pointsInRange.Count);

            quadTree.GetElementsInRange(point, range * 1.01f, pointsInRange);

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
        public void SimpleAnn()
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

        [Test]
        public void GetElementsInRangeWithEmptyTreeReturnsEmpty()
        {
            TTree tree = CreateTree(Enumerable.Empty<Vector2>());

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInRange(Vector2.zero, 10f, results);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetElementsInRangeWithNegativeRangeReturnsEmpty()
        {
            List<Vector2> points = new() { new Vector2(1f, 1f) };

            TTree tree = CreateTree(points);

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInRange(points[0], -1f, results);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetElementsInRangeWithZeroRangeReturnsOnlyExactMatches()
        {
            Vector2 target = new(5f, -3f);

            List<Vector2> points = new() { target, target, target + new Vector2(0.1f, 0f) };

            TTree tree = CreateTree(points);

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInRange(target, 0f, results);

            Vector2[] expected = { target, target };

            CollectionAssert.AreEquivalent(expected, results);
        }

        [Test]
        public void GetElementsInRangeWithMinimumRangeGreaterThanRangeReturnsEmpty()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f), new Vector2(1f, 1f) };

            TTree tree = CreateTree(points);

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInRange(Vector2.zero, 2f, results, minimumRange: 5f);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetElementsInRangeClearsResultsList()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f), new Vector2(1f, 0f) };

            TTree tree = CreateTree(points);

            Vector2 sentinel = new(123f, 456f);

            List<Vector2> results = new() { sentinel };

            tree.GetElementsInRange(Vector2.zero, 5f, results);

            Assert.IsFalse(results.Contains(sentinel));

            CollectionAssert.AreEquivalent(points, results);
        }

        [Test]
        public void GetElementsInBoundsReturnsOnlyContainedPoints()
        {
            List<Vector2> points = new()
            {
                new Vector2(-2f, -2f),
                new Vector2(0f, 0f),
                new Vector2(2f, 2f),
                new Vector2(10f, 10f),
            };

            TTree tree = CreateTree(points);

            Bounds bounds = new(new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 1f));

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInBounds(bounds, results);

            Vector2[] expected = { points[0], points[1], points[2] };

            CollectionAssert.AreEquivalent(expected, results);
        }

        [Test]
        public void GetElementsInBoundsWithNoIntersectionReturnsEmpty()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f) };

            TTree tree = CreateTree(points);

            Bounds bounds = new(new Vector3(100f, 100f, 0f), new Vector3(1f, 1f, 1f));

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetElementsInBounds(bounds, results);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetElementsInBoundsClearsResultsList()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f) };

            TTree tree = CreateTree(points);

            Vector2 sentinel = new(123f, 456f);

            List<Vector2> results = new() { sentinel };

            tree.GetElementsInBounds(new Bounds(Vector3.zero, Vector3.one * 10f), results);

            Assert.IsFalse(results.Contains(sentinel));

            CollectionAssert.AreEquivalent(points, results);
        }

        [Test]
        public void GetApproximateNearestNeighborsReturnsAllWhenRequestExceedsCount()
        {
            List<Vector2> points = new()
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
            };

            TTree tree = CreateTree(points);

            List<Vector2> results = new() { new Vector2(123f, 456f) };

            tree.GetApproximateNearestNeighbors(Vector2.zero, 10, results);

            CollectionAssert.AreEquivalent(points, results);
        }

        [Test]
        public void GetApproximateNearestNeighborsReturnsEmptyWhenCountZero()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f), new Vector2(1f, 1f) };

            TTree tree = CreateTree(points);

            Vector2 sentinel = new(123f, 456f);

            List<Vector2> results = new() { sentinel };

            tree.GetApproximateNearestNeighbors(Vector2.zero, 0, results);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetApproximateNearestNeighborsOnEmptyTreeReturnsEmpty()
        {
            TTree tree = CreateTree(Enumerable.Empty<Vector2>());

            Vector2 sentinel = new(123f, 456f);

            List<Vector2> results = new() { sentinel };

            tree.GetApproximateNearestNeighbors(Vector2.zero, 3, results);

            Assert.IsEmpty(results);
        }

        [Test]
        public void GetApproximateNearestNeighborsClearsResultsList()
        {
            List<Vector2> points = new() { new Vector2(0f, 0f), new Vector2(2f, 0f) };

            TTree tree = CreateTree(points);

            Vector2 sentinel = new(123f, 456f);

            List<Vector2> results = new() { sentinel };

            tree.GetApproximateNearestNeighbors(Vector2.zero, 1, results);

            Assert.IsFalse(results.Contains(sentinel));

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void AllPointsIdenticalQueries()
        {
            const int count = 64;
            Vector2 repeated = new(12.5f, -8.25f);
            List<Vector2> points = Enumerable.Repeat(repeated, count).ToList();

            TTree tree = CreateTree(points);

            List<Vector2> rangeResults = new() { new Vector2(1f, 2f) };
            tree.GetElementsInRange(repeated, 0f, rangeResults);
            Assert.AreEqual(count, rangeResults.Count);
            Assert.IsTrue(rangeResults.TrueForAll(candidate => candidate == repeated));

            Bounds bounds = new(
                new Vector3(repeated.x, repeated.y, 0f),
                new Vector3(0.1f, 0.1f, 1f)
            );
            List<Vector2> boundsResults = new() { new Vector2(-1f, -1f) };
            tree.GetElementsInBounds(bounds, boundsResults);
            Assert.AreEqual(count, boundsResults.Count);
            Assert.IsTrue(boundsResults.TrueForAll(candidate => candidate == repeated));

            List<Vector2> neighbors = new();
            tree.GetApproximateNearestNeighbors(repeated, count * 2, neighbors);
            Assert.IsNotEmpty(neighbors);
            Assert.AreEqual(repeated, neighbors[0]);
        }
    }
}
