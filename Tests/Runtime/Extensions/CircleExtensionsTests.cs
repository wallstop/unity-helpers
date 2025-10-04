namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class CircleExtensionsTests
    {
        [Test]
        public void EnumerateAreaReturnsPointsWithinCircle()
        {
            Circle circle = new(new UnityEngine.Vector2(0f, 0f), 2f);
            HashSet<FastVector3Int> points = new(circle.EnumerateArea());

            foreach (FastVector3Int point in points)
            {
                Assert.IsTrue(circle.Contains(new UnityEngine.Vector2(point.x, point.y)));
            }

            Assert.IsTrue(points.Contains(new FastVector3Int(0, 0, 0)));
            Assert.IsTrue(points.Contains(new FastVector3Int(1, 1, 0)));
            Assert.IsFalse(points.Contains(new FastVector3Int(2, 2, 0)));
        }
    }
}
