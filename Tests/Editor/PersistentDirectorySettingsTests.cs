namespace WallstopStudios.UnityHelpers.Tests.Editor
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor;

    public sealed class PersistentDirectorySettingsTests
    {
        [Test]
        public void GetPathsSortsByCountThenLastUsed()
        {
            PersistentDirectorySettings settings =
                ScriptableObject.CreateInstance<PersistentDirectorySettings>();

            string tool = "TestTool";
            string ctx = "Context";

            // B: 1 time, A: 3 times, C: 2 times
            settings.RecordPath(tool, ctx, "Assets/A");
            settings.RecordPath(tool, ctx, "Assets/A");
            settings.RecordPath(tool, ctx, "Assets/A");

            settings.RecordPath(tool, ctx, "Assets/C");
            settings.RecordPath(tool, ctx, "Assets/C");

            settings.RecordPath(tool, ctx, "Assets/B");

            DirectoryUsageData[] paths = settings.GetPaths(tool, ctx);
            Assert.IsNotNull(paths);
            Assert.GreaterOrEqual(paths.Length, 3);
            Assert.AreEqual("Assets/A", paths[0].path);
            Assert.AreEqual("Assets/C", paths[1].path);
            Assert.AreEqual("Assets/B", paths[2].path);
        }

        [Test]
        public void GetPathsTopOnlyRespectsLimit()
        {
            PersistentDirectorySettings settings =
                ScriptableObject.CreateInstance<PersistentDirectorySettings>();

            string tool = "TopOnlyTool";
            string ctx = "Context";

            settings.RecordPath(tool, ctx, "Assets/One");
            settings.RecordPath(tool, ctx, "Assets/One");
            settings.RecordPath(tool, ctx, "Assets/Two");
            settings.RecordPath(tool, ctx, "Assets/Three");

            DirectoryUsageData[] top2 = settings.GetPaths(tool, ctx, topOnly: true, topN: 2);
            Assert.IsNotNull(top2);
            Assert.AreEqual(2, top2.Length);
            Assert.AreEqual("Assets/One", top2[0].path);
        }
    }
#endif
}
