// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class UnityExtensionsConcaveHullDiagnosticsTests : GridTestBase
    {
        [Test]
        public void IsPositionInsideVector2DetectsInteriorAndExterior()
        {
            List<Vector2> squareHull = new()
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 4f),
                new Vector2(4f, 4f),
                new Vector2(4f, 0f),
            };

            Assert.IsTrue(
                UnityExtensions.IsPositionInside(squareHull, new Vector2(1f, 1f)),
                "Point inside the square should be detected as inside."
            );
            Assert.IsFalse(
                UnityExtensions.IsPositionInside(squareHull, new Vector2(5f, 1f)),
                "Point outside the square should be reported as outside."
            );
        }

        [Test]
        public void IsPositionInsideGridUsesGridCoordinates()
        {
            Grid grid = CreateGrid(out GameObject owner);
            Track(owner);

            List<FastVector3Int> squareHull = new()
            {
                new FastVector3Int(0, 0, 0),
                new FastVector3Int(0, 4, 0),
                new FastVector3Int(4, 4, 0),
                new FastVector3Int(4, 0, 0),
            };

            Assert.IsTrue(
                UnityExtensions.IsPositionInside(squareHull, new FastVector3Int(1, 1, 0), grid),
                "Grid-based helper should treat interior cells as inside the hull."
            );
            Assert.IsFalse(
                UnityExtensions.IsPositionInside(squareHull, new FastVector3Int(6, 1, 0), grid),
                "Grid-based helper should reject cells outside the hull bounds."
            );
        }
    }
}
