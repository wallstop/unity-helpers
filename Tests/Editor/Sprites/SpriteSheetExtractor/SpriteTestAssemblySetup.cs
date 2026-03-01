// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using NUnit.Framework;

    /// <summary>
    /// Assembly-level setup fixture that preloads all shared sprite test assets once
    /// before any tests in this assembly run. This eliminates repeated AssetDatabase
    /// calls during individual test execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By using <see cref="SetUpFixtureAttribute"/>, this class ensures that
    /// <see cref="SharedSpriteTestFixtures.PreloadAllAssets"/> is called exactly once
    /// at the start of the test assembly execution, and
    /// <see cref="SharedSpriteTestFixtures.ReleaseAllCachedAssets"/> is called once
    /// at the end.
    /// </para>
    /// <para>
    /// This significantly improves test performance by:
    /// <list type="bullet">
    /// <item>Loading all textures once instead of per-test</item>
    /// <item>Caching TextureImporter references</item>
    /// <item>Caching directory Object references</item>
    /// </list>
    /// </para>
    /// </remarks>
    [SetUpFixture]
    public sealed class SpriteTestAssemblySetup
    {
        /// <summary>
        /// Called once before any tests in this assembly run.
        /// Preloads all shared sprite test assets into cache.
        /// </summary>
        [OneTimeSetUp]
        public void AssemblySetUp()
        {
            SharedSpriteTestFixtures.PreloadAllAssets();
        }

        /// <summary>
        /// Called once after all tests in this assembly have completed.
        /// Releases all cached asset references.
        /// </summary>
        [OneTimeTearDown]
        public void AssemblyTearDown()
        {
            SharedSpriteTestFixtures.ReleaseAllCachedAssets();
        }
    }
#endif
}
