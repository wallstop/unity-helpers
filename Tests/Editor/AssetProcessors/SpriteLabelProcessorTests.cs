// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;

    [TestFixture]
    public sealed class SpriteLabelProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            SpriteLabelProcessor.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            SpriteLabelProcessor.ResetForTesting();
        }

        [Test]
        public void EnqueueImportedPathsForTestingDeduplicatesCandidatePaths()
        {
            SpriteLabelProcessor.EnqueueImportedPathsForTesting(
                new[]
                {
                    "Assets/Sprites/Hero.png",
                    "Assets/Sprites/Hero.png",
                    "Assets/Sprites/hero.PNG",
                    "Assets/Sprites/Villain.jpg",
                    "Assets/Sprites/Notes.txt",
                    "Packages/com.wallstop-studios.unity-helpers/Icons/Icon.png",
                }
            );

            string[] normalizedPaths = SpriteLabelProcessor
                .SnapshotPendingImportedPathsForTesting()
                .Select(path => path.ToLowerInvariant())
                .ToArray();

            Assert.AreEqual(
                2,
                SpriteLabelProcessor.PendingImportedPathCountForTesting,
                "Only unique sprite candidate assets under Assets/ should remain queued."
            );
            CollectionAssert.AreEquivalent(
                new[] { "assets/sprites/hero.png", "assets/sprites/villain.jpg" },
                normalizedPaths,
                "The pending set should keep the expected unique candidate paths only."
            );
        }
    }
}
