namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    public sealed class TexturePlatformNameHelperTests
    {
        [Test]
        public void ReturnsDefaultAndStandalone()
        {
            string[] names = TexturePlatformNameHelper.GetKnownPlatformNames();
            CollectionAssert.Contains(names, "DefaultTexturePlatform");
            CollectionAssert.Contains(names, "Standalone");
        }

        [Test]
        public void IncludesIPhoneForIOSGroupWhenAvailable()
        {
            string[] names = TexturePlatformNameHelper.GetKnownPlatformNames();
            // Not all editor installs have iOS module; allow either iPhone or fallback iOS string
            bool hasIPhone = System.Array.IndexOf(names, "iPhone") >= 0;
            bool hasIOS = System.Array.IndexOf(names, "iOS") >= 0;
            Assert.IsTrue(hasIPhone || hasIOS);
        }

        [Test]
        public void CachesResultsAndIsSorted()
        {
            string[] a = TexturePlatformNameHelper.GetKnownPlatformNames();
            string[] b = TexturePlatformNameHelper.GetKnownPlatformNames();
            // Expect the exact same reference due to caching
            Assert.AreSame(a, b);

            // Verify ascending ordinal order
            for (int i = 1; i < a.Length; i++)
            {
                Assert.LessOrEqual(
                    string.Compare(a[i - 1], a[i], System.StringComparison.Ordinal),
                    0
                );
            }
        }
    }
#endif
}
