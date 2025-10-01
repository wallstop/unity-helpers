namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class IterationHelpersTests
    {
        [Test]
        public void IndexOverTwoDimensionalArrayReturnsAllCoordinates()
        {
            int[,] grid = new int[2, 3];

            (int, int)[] indices = grid.IndexOver().ToArray();

            Assert.That(
                indices,
                Is.EqualTo(new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2) })
            );
        }

        [Test]
        public void IndexOverThreeDimensionalArrayReturnsAllCoordinates()
        {
            int[,,] grid = new int[2, 2, 2];

            HashSet<(int, int, int)> indices = grid.IndexOver().ToHashSet();

            Assert.That(
                indices,
                Is.EquivalentTo(
                    new[]
                    {
                        (0, 0, 0),
                        (0, 0, 1),
                        (0, 1, 0),
                        (0, 1, 1),
                        (1, 0, 0),
                        (1, 0, 1),
                        (1, 1, 0),
                        (1, 1, 1),
                    }
                )
            );
        }
    }
}
