namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    public sealed class EditorUiSuppressionTests : CommonTestBase
    {
        private const string TestRoot = "Assets/Temp/EditorUiSuppressionTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(TestRoot);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void EditorUiSuppressIsSetByBaseClass()
        {
            Assert.IsTrue(
                EditorUi.Suppress,
                "EditorUi.Suppress should be true during test execution."
            );
        }

        [Test]
        public void ConfirmReturnsTrueWhenSuppressed()
        {
            bool result = EditorUi.Confirm("Test Dialog", "This should not show.", "OK", "Cancel");
            Assert.IsTrue(result, "Confirm should return default (true) when suppressed.");
        }

        [Test]
        public void ConfirmReturnsCustomDefaultWhenSuppressed()
        {
            bool result = EditorUi.Confirm(
                "Test Dialog",
                "This should not show.",
                "OK",
                "Cancel",
                defaultWhenSuppressed: false
            );
            Assert.IsFalse(result, "Confirm should return custom default (false) when suppressed.");
        }

        [Test]
        public void InfoDoesNotThrowWhenSuppressed()
        {
            Assert.DoesNotThrow(() => EditorUi.Info("Test", "Message"));
        }

        [Test]
        public void ShowProgressDoesNotThrowWhenSuppressed()
        {
            Assert.DoesNotThrow(() => EditorUi.ShowProgress("Title", "Info", 0.5f));
            EditorUi.ClearProgress();
        }

        [Test]
        public void CancelableProgressReturnsFalseWhenSuppressed()
        {
            bool cancelled = EditorUi.CancelableProgress("Title", "Info", 0.5f);
            Assert.IsFalse(cancelled, "CancelableProgress should return false when suppressed.");
            EditorUi.ClearProgress();
        }

        [UnityTest]
        public IEnumerator TryCopyAssetSilentCopiesFileWithoutDialog()
        {
            string sourcePath = TestRoot + "/source.txt";
            string destPath = TestRoot + "/dest.txt";

            string absoluteSource = System
                .IO.Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    sourcePath
                )
                .Replace('\\', '/');

            System.IO.File.WriteAllText(absoluteSource, "test content");
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            bool success = TryCopyAssetSilent(sourcePath, destPath);
            yield return null;

            Assert.IsTrue(success, "TryCopyAssetSilent should succeed.");

            string absoluteDest = System
                .IO.Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    destPath
                )
                .Replace('\\', '/');

            Assert.IsTrue(System.IO.File.Exists(absoluteDest), "Destination file should exist.");
            Assert.AreEqual("test content", System.IO.File.ReadAllText(absoluteDest));
        }

        [UnityTest]
        public IEnumerator TryCopyAssetSilentReturnsFalseForMissingSource()
        {
            string sourcePath = TestRoot + "/nonexistent.txt";
            string destPath = TestRoot + "/dest2.txt";

            bool success = TryCopyAssetSilent(sourcePath, destPath);
            yield return null;

            Assert.IsFalse(success, "TryCopyAssetSilent should return false for missing source.");
        }

        [Test]
        public void TryCopyAssetSilentReturnsFalseForNullPaths()
        {
            Assert.IsFalse(TryCopyAssetSilent(null, "dest"));
            Assert.IsFalse(TryCopyAssetSilent("source", null));
            Assert.IsFalse(TryCopyAssetSilent(string.Empty, "dest"));
            Assert.IsFalse(TryCopyAssetSilent("source", string.Empty));
        }

        private static void EnsureFolder(string relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath))
            {
                return;
            }

            relPath = relPath.Replace('\\', '/');

            // Ensure the folder exists on disk first to prevent AssetDatabase.CreateFolder from failing
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = System.IO.Path.Combine(projectRoot, relPath);
                if (!System.IO.Directory.Exists(absoluteDirectory))
                {
                    System.IO.Directory.CreateDirectory(absoluteDirectory);
                }
            }

            // Then ensure it's registered in AssetDatabase
            if (AssetDatabase.IsValidFolder(relPath))
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
                    string result = AssetDatabase.CreateFolder(cur, parts[i]);
                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogWarning(
                            $"EnsureFolder: Failed to create folder '{next}' in AssetDatabase (parent: '{cur}')"
                        );
                    }
                }
                cur = next;
            }
        }
    }
#endif
}
