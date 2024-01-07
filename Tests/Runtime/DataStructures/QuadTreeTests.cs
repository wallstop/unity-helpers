namespace UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataStructure;
    using Core.Extension;
    using Core.Helper;
    using Core.Random;
    using NUnit.Framework;
    using UnityEngine;

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

            QuadTree<Vector2> quadTree = new(points, _ => _, points.GetBounds()!.Value);

            HashSet<Vector2> pointsInRange = quadTree.GetElementsInRange(center, radius).ToHashSet();
            Assert.IsTrue(points.SetEquals(pointsInRange), "Found {0} points in range, expected {1}.", pointsInRange.Count, points.Count);
            // Translate by a unit-square - there should be no points in this range
            Vector2 offset = center;
            offset.x -= radius * 2;
            offset.y -= radius * 2;

            pointsInRange = quadTree.GetElementsInRange(offset, radius).ToHashSet();
            Assert.AreEqual(
                0, pointsInRange.Count, "Found {0} points within {1} range of {2} (original center {3})",
                pointsInRange.Count, radius, offset, center);
        }

        [Test]
        public void SimplePointOutsideRange()
        {
            Vector2 point = new(Random.NextFloat(-100, 100), Random.NextFloat(-100, 100));
            // TODO
        }
    }
}
