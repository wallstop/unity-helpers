// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Model;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class DirectionExtensionsTests : CommonTestBase
    {
        [Test]
        public void OppositeReturnsExpectedValues()
        {
            Assert.AreEqual(Direction.South, Direction.North.Opposite());
            Assert.AreEqual(Direction.NorthWest, Direction.SouthEast.Opposite());
            Assert.AreEqual(Direction.None, Direction.None.Opposite());
        }

        [Test]
        public void AsVectorConversionsAreConsistent()
        {
            Assert.AreEqual(Vector2Int.up, Direction.North.AsVector2Int());
            Assert.AreEqual(Vector2Int.right, Direction.East.AsVector2Int());
            Assert.AreEqual(Vector2.up, Direction.North.AsVector2());
            Assert.AreEqual(Vector2.zero, Direction.None.AsVector2());
        }

        [Test]
        public void SplitReturnsAllFlags()
        {
            Direction compound = Direction.North | Direction.East | Direction.South;
            List<Direction> parts = new(compound.Split());
            Assert.Contains(Direction.North, parts);
            Assert.Contains(Direction.East, parts);
            Assert.Contains(Direction.South, parts);
            Assert.AreEqual(3, parts.Count);

            List<Direction> none = new(Direction.None.Split());
            Assert.AreEqual(1, none.Count);
            Assert.AreEqual(Direction.None, none[0]);
        }

        [Test]
        public void CombineAggregatesDirections()
        {
            Direction combined = new[]
            {
                Direction.North,
                Direction.East,
                Direction.West,
            }.Combine();

            Assert.IsTrue(combined.HasFlag(Direction.North));
            Assert.IsTrue(combined.HasFlag(Direction.East));
            Assert.IsTrue(combined.HasFlag(Direction.West));
        }

        [Test]
        public void AsDirectionChoosesNearestCardinal()
        {
            Assert.AreEqual(Direction.None, Vector2.zero.AsDirection());
            Assert.AreEqual(Direction.North, Vector2.up.AsDirection());
            Assert.AreEqual(Direction.SouthWest, new Vector2(-0.5f, -0.5f).AsDirection());

            Vector2 vector = Quaternion.AngleAxis(10f, Vector3.forward) * Vector2.up;
            Assert.AreEqual(Direction.North, vector.AsDirection(preferAngles: true));
        }
    }
}
