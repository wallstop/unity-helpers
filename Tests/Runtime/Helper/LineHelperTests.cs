namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class LineHelperTests
    {
        [Test]
        public void SimplifyPreciseReturnsOriginalWhenInsufficientPoints()
        {
            List<Vector2> points = new() { Vector2.zero, Vector2.one };

            List<Vector2> result = LineHelper.SimplifyPrecise(points, 0.5);

            Assert.That(result, Is.EqualTo(points));
        }

        [Test]
        public void SimplifyPreciseRemovesRedundantColinearPoints()
        {
            List<Vector2> points = new()
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(2f, 0f),
            };

            List<Vector2> result = LineHelper.SimplifyPrecise(points, 0.01);

            Assert.That(result, Is.EqualTo(new[] { points[0], points[^1] }));
        }

        [Test]
        public void SimplifyPreservesSignificantDeviation()
        {
            List<Vector2> points = new()
            {
                new Vector2(0f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(1f, 0f),
            };

            List<Vector2> result = LineHelper.Simplify(points, 0.2f);

            Assert.That(result, Is.EqualTo(points));
        }

        [Test]
        public void SimplifyReturnsCopyWhenEpsilonZero()
        {
            List<Vector2> points = new()
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(2f, 2f),
            };

            List<Vector2> result = LineHelper.Simplify(points, 0f);

            Assert.AreNotSame(points, result);
            Assert.That(result, Is.EqualTo(points));
        }
    }
}
