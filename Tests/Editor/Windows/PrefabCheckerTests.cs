namespace WallstopStudios.UnityHelpers.Tests.Editor.Windows
{
#if UNITY_EDITOR
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;

    public sealed class PrefabCheckerTests
    {
        private const string Root = "Assets/Temp/PrefabCheckerTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public void TearDown()
        {
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

            GameObject go = new("DummyPrefab");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                AssetDatabase.Refresh();

                PrefabChecker checker = ScriptableObject.CreateInstance<PrefabChecker>();

                FieldInfo assetPathsField = typeof(PrefabChecker).GetField(
                    "_assetPaths",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.IsNotNull(assetPathsField, "_assetPaths field not found");

                var list = new System.Collections.Generic.List<string> { "Assets" };
                assetPathsField.SetValue(checker, list);

                MethodInfo runChecks = typeof(PrefabChecker).GetMethod(
                    "RunChecksImproved",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.IsNotNull(runChecks, "RunChecksImproved method not found");

                Assert.DoesNotThrow(() => runChecks.Invoke(checker, null));
            }
            finally
            {
                Object.DestroyImmediate(go);
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.Refresh();
            }
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
