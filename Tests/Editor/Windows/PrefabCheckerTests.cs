namespace WallstopStudios.UnityHelpers.Tests.Windows
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class PrefabCheckerTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/PrefabCheckerTests";

        // Regex patterns for expected error messages from PrefabChecker
        private static readonly Regex NoAssetPathsErrorPattern = new(
            @"\[PrefabChecker\].*No asset paths specified",
            RegexOptions.Compiled
        );

        private static readonly Regex InvalidPathsErrorPattern = new(
            @"\[PrefabChecker\].*None of the specified paths are valid folders",
            RegexOptions.Compiled
        );

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            // Always reset ignoreFailingMessages to prevent test pollution
            LogAssert.ignoreFailingMessages = false;
            base.TearDown();
            // Clean up only tracked folders/assets that this test created
            CleanupTrackedFoldersAndAssets();
        }

        /// <summary>
        /// Executes an action while ignoring all log assertions.
        /// Use this for tests that only care about "does not throw" behavior
        /// and should not be affected by unrelated log messages from project state.
        /// </summary>
        private static void ExecuteIgnoringLogs(Action action)
        {
            bool previousValue = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                action();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousValue;
            }
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

            // Only scan our test folder to avoid errors from other prefabs in the project
            // that may have null elements (such as SpriteCache prefabs with missing sprites)
            List<string> list = new() { Root };
            checker._assetPaths = list;

            Assert.DoesNotThrow(() => checker.RunChecksImproved());
        }

        [Test]
        public void RunChecksOnEmptyFolderCompletesWithoutError()
        {
            string emptySubFolder = Path.Combine(Root, "EmptyFolder").SanitizePath();
            EnsureFolder(emptySubFolder);
            AssetDatabase.Refresh();

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { emptySubFolder };

            Assert.DoesNotThrow(() => checker.RunChecksImproved());
        }

        [Test]
        [TestCase("")]
        [TestCase("   ")]
        public void DataPathConversionHandlesInvalidInputs(string invalidPath)
        {
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(invalidPath);
            // Empty or whitespace paths should return null or empty
            Assert.IsTrue(
                string.IsNullOrEmpty(result),
                $"Expected null or empty for invalid path '{invalidPath}', got '{result}'"
            );
        }

        [Test]
        public void RunChecksWithNullAssetPathsLogsErrorAndDoesNotThrow()
        {
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = null;

            // Expect the error log that production code emits for null/empty asset paths
            LogAssert.Expect(LogType.Error, NoAssetPathsErrorPattern);

            // Should handle null gracefully without throwing
            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                "RunChecksImproved() should not throw when asset paths are null"
            );
        }

        [Test]
        public void RunChecksWithEmptyAssetPathsListLogsErrorAndDoesNotThrow()
        {
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string>();

            // Expect the error log that production code emits for null/empty asset paths
            LogAssert.Expect(LogType.Error, NoAssetPathsErrorPattern);

            // Should handle empty list gracefully without throwing
            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                "RunChecksImproved() should not throw when asset paths list is empty"
            );
        }

        [Test]
        public void RunChecksOnSingleValidPrefabCompletesSuccessfully()
        {
            string prefabPath = Path.Combine(Root, "SingleValid.prefab").SanitizePath();
            EnsureFolder(Path.GetDirectoryName(prefabPath).SanitizePath());

            // Create a valid prefab with a simple component
            GameObject go = Track(new GameObject("SingleValidPrefab"));
            go.AddComponent<BoxCollider>();
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            TrackAssetPath(prefabPath);
            AssetDatabase.Refresh();

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { Root };

            Assert.DoesNotThrow(() => checker.RunChecksImproved());
        }

        [Test]
        public void RunChecksOnNonExistentPathLogsErrorAndDoesNotThrow()
        {
            const string nonExistentPath = "Assets/NonExistent/Path/That/DoesNotExist";
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { nonExistentPath };

            // Expect the error log that production code emits when no valid folders are found
            LogAssert.Expect(LogType.Error, InvalidPathsErrorPattern);

            // Should handle non-existent paths gracefully without throwing
            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                $"RunChecksImproved() should not throw when path '{nonExistentPath}' does not exist"
            );
        }

        [Test]
        [TestCase(null, TestName = "NullPathInList")]
        [TestCase("", TestName = "EmptyStringPathInList")]
        [TestCase("   ", TestName = "WhitespacePathInList")]
        public void RunChecksWithInvalidPathEntriesLogsErrorAndDoesNotThrow(string invalidPath)
        {
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { invalidPath };

            // Expect the error log - these invalid entries result in no valid folders
            LogAssert.Expect(LogType.Error, InvalidPathsErrorPattern);

            // Should handle invalid path entries gracefully without throwing
            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                $"RunChecksImproved() should not throw when list contains invalid path: '{invalidPath ?? "null"}'"
            );
        }

        [Test]
        public void RunChecksWithMixedValidAndInvalidPathsProcessesValidOnes()
        {
            string prefabPath = Path.Combine(Root, "MixedTest.prefab").SanitizePath();
            EnsureFolder(Path.GetDirectoryName(prefabPath).SanitizePath());

            GameObject go = Track(new GameObject("MixedTestPrefab"));
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            TrackAssetPath(prefabPath);
            AssetDatabase.Refresh();

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            // Mix of valid and invalid paths
            checker._assetPaths = new List<string>
            {
                "Assets/NonExistent/Invalid/Path",
                Root, // This one is valid
                "",
                null,
                "   ",
            };

            // Should NOT log an error because at least one path (Root) is valid
            // The invalid paths should be silently filtered out
            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                "RunChecksImproved() should process valid paths even when list contains invalid entries"
            );
        }

        [Test]
        public void RunChecksWithMultipleNonExistentPathsLogsAllInError()
        {
            const string path1 = "Assets/NonExistent/Path1";
            const string path2 = "Assets/NonExistent/Path2";
            const string path3 = "Assets/Another/Missing/Path";

            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { path1, path2, path3 };

            // Expect the error log listing all invalid paths
            LogAssert.Expect(LogType.Error, InvalidPathsErrorPattern);

            Assert.DoesNotThrow(
                () => checker.RunChecksImproved(),
                "RunChecksImproved() should not throw when multiple paths are non-existent"
            );
        }

        [Test]
        public void RunChecksAcceptsAssetsRootPathDirectly()
        {
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { "Assets" };

            // Assets root should always be valid - no error expected from our validation.
            // However, scanning Assets root may trigger errors from other prefabs in the project.
            // Use ExecuteIgnoringLogs to isolate this test from project state.
            ExecuteIgnoringLogs(() =>
            {
                Assert.DoesNotThrow(
                    () => checker.RunChecksImproved(),
                    "RunChecksImproved() should accept 'Assets' as a valid root path"
                );
            });
        }

        [Test]
        public void RunChecksWithNullAssetPathsDoesNotThrowRegardlessOfProjectState()
        {
            // This test verifies the "does not throw" behavior in isolation from log assertions.
            // It complements RunChecksWithNullAssetPathsLogsErrorAndDoesNotThrow which verifies the log.
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = null;

            ExecuteIgnoringLogs(() =>
            {
                Assert.DoesNotThrow(
                    () => checker.RunChecksImproved(),
                    "RunChecksImproved() should not throw when asset paths are null"
                );
            });
        }

        [Test]
        public void RunChecksWithEmptyAssetPathsDoesNotThrowRegardlessOfProjectState()
        {
            // This test verifies the "does not throw" behavior in isolation from log assertions.
            // It complements RunChecksWithEmptyAssetPathsListLogsErrorAndDoesNotThrow which verifies the log.
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string>();

            ExecuteIgnoringLogs(() =>
            {
                Assert.DoesNotThrow(
                    () => checker.RunChecksImproved(),
                    "RunChecksImproved() should not throw when asset paths list is empty"
                );
            });
        }

        [Test]
        public void RunChecksOnNonExistentPathDoesNotThrowRegardlessOfProjectState()
        {
            // This test verifies the "does not throw" behavior in isolation from log assertions.
            // It complements RunChecksOnNonExistentPathLogsErrorAndDoesNotThrow which verifies the log.
            const string nonExistentPath = "Assets/NonExistent/Path/That/DoesNotExist";
            PrefabChecker checker = Track(ScriptableObject.CreateInstance<PrefabChecker>());
            checker._assetPaths = new List<string> { nonExistentPath };

            ExecuteIgnoringLogs(() =>
            {
                Assert.DoesNotThrow(
                    () => checker.RunChecksImproved(),
                    $"RunChecksImproved() should not throw when path '{nonExistentPath}' does not exist"
                );
            });
        }
    }
#endif
}
