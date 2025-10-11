namespace WallstopStudios.UnityHelpers.Tests.Editor.Windows
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class PrefabCheckerTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/PrefabCheckerTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
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
            string prefabPath = Path.Combine(Root, "Dummy.prefab").Replace('\\', '/');
            EnsureFolder(Path.GetDirectoryName(prefabPath).Replace('\\', '/'));

            GameObject go = Track(new GameObject("DummyPrefab"));
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            AssetDatabase.Refresh();

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());

            var list = new System.Collections.Generic.List<string> { "Assets" };
            checker._assetPaths = list;

            Assert.DoesNotThrow(() => checker.RunChecksImproved());
        }

        private static void EnsureFolder(string relPath)
        {
            relPath = relPath.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(relPath))
            {
                return;
            }

            string[] parts = relPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }
    }
#endif
}
