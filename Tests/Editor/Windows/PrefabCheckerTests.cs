namespace WallstopStudios.UnityHelpers.Tests.Windows
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class PrefabCheckerTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/PrefabCheckerTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            // Clean up only tracked folders/assets that this test created
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void DataPathConvertsToAssets()
        {
            string dataPath = Application.dataPath;
            string rel = DirectoryHelper.AbsoluteToUnityRelativePath(dataPath);
            Assert.IsNotNull(rel);
            Assert.IsNotEmpty(rel);
            Assert.AreEqual("Assets", rel, "Root Assets conversion should be exactly 'Assets'.");
        }

        [Test]
        public void RunChecksAcceptsAssetsRoot()
        {
            string prefabPath = Path.Combine(Root, "Dummy.prefab").SanitizePath();
            EnsureFolder(Path.GetDirectoryName(prefabPath).SanitizePath());

            GameObject go = Track(new GameObject("DummyPrefab"));
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            TrackAssetPath(prefabPath);
            AssetDatabase.Refresh();

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());

            List<string> list = new() { "Assets" };
            checker._assetPaths = list;

            Assert.DoesNotThrow(() => checker.RunChecksImproved());
        }
    }
#endif
}
