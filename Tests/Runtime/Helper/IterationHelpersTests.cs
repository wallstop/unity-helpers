// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class IterationHelpersTests : CommonTestBase
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

        [Test]
        public void IndexOver2DArrayWithBufferReturnsBuffer()
        {
            int[,] array = new int[2, 2];
            List<(int, int)> buffer = new();

            List<(int, int)> result = array.IndexOver(buffer);

            Assert.AreSame(buffer, result);
            Assert.AreEqual(4, result.Count);
        }

        [Test]
        public void IndexOver2DArrayWithBufferClearsBufferFirst()
        {
            int[,] array = new int[2, 2];
            List<(int, int)> buffer = new() { (999, 999), (888, 888) };

            List<(int, int)> result = array.IndexOver(buffer);

            Assert.AreEqual(4, result.Count);
            Assert.IsFalse(result.Contains((999, 999)), string.Join(",", result));
        }

        [Test]
        public void IndexOver2DArrayWithZeroRowsReturnsEmpty()
        {
            int[,] array = new int[0, 5];
            List<(int, int)> indices = array.IndexOver().ToList();

            Assert.AreEqual(0, indices.Count);
        }

        [Test]
        public void IndexOver2DArrayWithZeroColumnsReturnsEmpty()
        {
            int[,] array = new int[5, 0];
            List<(int, int)> indices = array.IndexOver().ToList();

            Assert.AreEqual(0, indices.Count);
        }

        [Test]
        public void IndexOver3DArrayWithBufferReturnsBuffer()
        {
            int[,,] array = new int[2, 2, 2];
            List<(int, int, int)> buffer = new();

            List<(int, int, int)> result = array.IndexOver(buffer);

            Assert.AreSame(buffer, result);
            Assert.AreEqual(8, result.Count);
        }

        [Test]
        public void IndexOver3DArrayWithBufferClearsBufferFirst()
        {
            int[,,] array = new int[2, 2, 2];
            List<(int, int, int)> buffer = new() { (999, 999, 999) };

            List<(int, int, int)> result = array.IndexOver(buffer);

            Assert.AreEqual(8, result.Count);
            Assert.IsFalse(result.Contains((999, 999, 999)), string.Join(",", result));
        }

        [Test]
        public void IndexOver3DArrayWithZeroFirstDimensionReturnsEmpty()
        {
            int[,,] array = new int[0, 3, 4];
            List<(int, int, int)> indices = array.IndexOver().ToList();

            Assert.AreEqual(0, indices.Count);
        }

        [Test]
        public void IndexOver2DArrayCanBeUsedToAccessElements()
        {
            int[,] array = new int[3, 3];
            int counter = 0;
            foreach ((int i, int j) in array.IndexOver())
            {
                array[i, j] = counter++;
            }

            Assert.AreEqual(0, array[0, 0]);
            Assert.AreEqual(4, array[1, 1]);
            Assert.AreEqual(8, array[2, 2]);
        }

        [Test]
        public void IndexOver3DArrayCanBeUsedToAccessElements()
        {
            int[,,] array = new int[2, 2, 2];
            int counter = 0;
            foreach ((int i, int j, int k) in array.IndexOver())
            {
                array[i, j, k] = counter++;
            }

            Assert.AreEqual(0, array[0, 0, 0]);
            Assert.AreEqual(3, array[0, 1, 1]);
            Assert.AreEqual(7, array[1, 1, 1]);
        }

        [Test]
        public void IndexOver2DArrayWithLargeArrayYieldsAllIndices()
        {
            double[,] array = new double[50, 50];
            List<(int, int)> indices = array.IndexOver().ToList();

            Assert.AreEqual(2500, indices.Count);
            Assert.That(indices, Does.Contain((0, 0)));
            Assert.That(indices, Does.Contain((49, 49)));
        }

        [Test]
        public void IndexOver3DArrayWithAsymmetricDimensionsYieldsCorrectCount()
        {
            int[,,] array = new int[2, 3, 5];
            List<(int, int, int)> indices = array.IndexOver().ToList();

            Assert.AreEqual(30, indices.Count);
        }
    }
}
