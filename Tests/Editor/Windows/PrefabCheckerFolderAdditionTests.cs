namespace WallstopStudios.UnityHelpers.Tests.Windows
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class PrefabCheckerFolderAdditionTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/PrefabCheckerFolderAdditionTests";

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
        public void TryAddFolderFromAbsoluteAddsAssetsRoot()
        {
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            bool added = checker.TryAddFolderFromAbsolute(Application.dataPath);
            Assert.IsTrue(added, "Expected adding from Application.dataPath to succeed.");
            CollectionAssert.Contains(checker._assetPaths, "Assets");
        }

        [Test]
        public void AddAssetFolderAddsValidFolder()
        {
            string sub = Path.Combine(Root, "Sub").Replace('\\', '/');
            EnsureFolder(sub);

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            bool added = checker.AddAssetFolder(sub);
            Assert.IsTrue(added, "Expected valid Unity folder to be added.");
            CollectionAssert.Contains(checker._assetPaths, sub);
        }

        [Test]
        public void AddAssetFolderDedupesExisting()
        {
            string sub = Path.Combine(Root, "Dup").Replace('\\', '/');
            EnsureFolder(sub);

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            bool first = checker.AddAssetFolder(sub);
            bool second = checker.AddAssetFolder(sub);
            Assert.IsTrue(first, "First add should succeed.");
            Assert.IsFalse(second, "Second add should be rejected as duplicate.");
            Assert.AreEqual(1, checker._assetPaths.Count, "Only one entry should exist.");
        }

        [Test]
        public void AddAssetFolderRejectsInvalidFolder()
        {
            string invalid = Path.Combine(Root, "DoesNotExist").Replace('\\', '/');
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            bool added = checker.AddAssetFolder(invalid);
            Assert.IsFalse(added, "Invalid folder should not be added.");
            CollectionAssert.DoesNotContain(checker._assetPaths, invalid);
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
