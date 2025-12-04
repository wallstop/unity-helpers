namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using Object = UnityEngine.Object;

    public sealed class ScriptableObjectSingletonCreatorTests : CommonTestBase
    {
        private const string TestRoot = "Assets/Resources/CreatorTests";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.VerboseLogging = true;
            ScriptableObjectSingletonCreator.TypeFilter = static type =>
                type == typeof(CaseMismatch)
                || type == typeof(Duplicate)
                || type == typeof(A.NameCollision)
                || type == typeof(B.NameCollision)
                || type == typeof(RetrySingleton)
                || type == typeof(FileBlockSingleton)
                || type == typeof(NoRetrySingleton);
            EnsureFolder("Assets/Resources");
            EnsureFolder(TestRoot);
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            yield break;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            IEnumerator baseEnumerator = base.UnityTearDown();
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            // Clean up any assets created under our test root
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { TestRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            // Try to delete empty folders bottom-up
            TryDeleteFolder(TestRoot);
            TryDeleteFolder("Assets/Resources/Collision");
            TryDeleteFolder("Assets/Resources/CaseTest");
            TryDeleteFolder(TestRoot + "/Retry");
            TryDeleteFolder(TestRoot + "/Retry 1");
            TryDeleteFolder(TestRoot + "/FileBlock");
            TryDeleteFolder(TestRoot + "/FileBlock 1");
            DeleteFileIfExists(TestRoot + "/FileBlock");
            TryDeleteFolder(TestRoot + "/NoRetry");
            TryDeleteFolder(TestRoot + "/NoRetry 1");
            DeleteFileIfExists(TestRoot + "/NoRetry");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ScriptableObjectSingletonCreator.TypeFilter = null;
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
        }

        [UnityTest]
        public IEnumerator DoesNotCreateDuplicateSubfolderOnCaseMismatch()
        {
            // Arrange: create wrong-cased subfolder under Resources
            EnsureFolder("Assets/Resources/cASEtest");
            string assetPath = "Assets/Resources/cASEtest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(assetPath);
            LogAssert.ignoreFailingMessages = true;

            // Act: trigger creation for a singleton targeting "CaseTest" path
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no duplicate folder created and asset placed in reused folder
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/Resources/cASEtest"));
            Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Resources/CaseTest 1"));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null);
        }

        [UnityTest]
        public IEnumerator SkipsCreationWhenTargetPathOccupied()
        {
            // Arrange: Create an occupying asset at the target path
            string targetFolder = TestRoot;
            EnsureFolder(targetFolder);
            string occupiedPath = targetFolder + "/Duplicate.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(occupiedPath) == null)
            {
                TextAsset ta = new("occupied");
                AssetDatabase.CreateAsset(ta, occupiedPath);
            }

            // Act: run ensure and expect a warning about occupied target
            LogAssert.Expect(LogType.Warning, new Regex("target path already occupied"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no duplicate asset created alongside
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(targetFolder + "/Duplicate 1.asset") == null
            );
        }

        [UnityTest]
        public IEnumerator WarnsOnTypeNameCollision()
        {
            // Arrange: ensure collision folder exists
            EnsureFolder("Assets/Resources/CreatorTests/Collision");

            // Act: ensure logs a collision warning and does not create the overlapping asset
            LogAssert.Expect(LogType.Warning, new Regex("Type name collision"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no asset created at the ambiguous path
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/CreatorTests/Collision/NameCollision.asset"
                ) == null
            );
        }

        [UnityTest]
        public IEnumerator EnsureSingletonAssetsIsIdempotent()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            string firstGuid = AssetDatabase.AssetPathToGUID(targetPath);
            Assert.IsFalse(string.IsNullOrEmpty(firstGuid));

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            string secondGuid = AssetDatabase.AssetPathToGUID(targetPath);
            Assert.AreEqual(firstGuid, secondGuid);
        }

        [UnityTest]
        public IEnumerator SkipsEnsureInsideAssetImportWorkerProcess()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = static () => true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) == null);
            }
            finally
            {
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
            }

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
        }

        [UnityTest]
        public IEnumerator EnvironmentVariablesTriggerWorkerDetection(
            [ValueSource(nameof(AssetImportWorkerEnvironmentScenarios))] string environmentVariable
        )
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            string originalValue = Environment.GetEnvironmentVariable(environmentVariable);
            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            try
            {
                Environment.SetEnvironmentVariable(environmentVariable, "1");
                ScriptableObjectSingletonCreator.ResetAssetImportWorkerDetectionStateForTests();
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = null;

                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;

                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) == null);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariable, originalValue);
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
                ScriptableObjectSingletonCreator.ResetAssetImportWorkerDetectionStateForTests();
            }

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
        }

        [UnityTest]
        public IEnumerator DetectorExceptionsDoNotBlockEnsure()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = static () =>
                throw new InvalidOperationException("detector failure");

            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
            }
            finally
            {
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
            }
        }

        [UnityTest]
        public IEnumerator RetriesCreationAfterTemporaryFolderBlock()
        {
            string retryFolder = TestRoot + "/Retry";
            string retryAsset = retryFolder + "/RetrySingleton.asset";
            string blockerMeta = retryFolder + ".meta";
            string retryFolderVariant = retryFolder + " 1";

            // Reset retry state to ensure fresh start
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();

            // Thorough cleanup of all possible states
            CleanupRetryTestState(retryFolder, retryAsset, blockerMeta, retryFolderVariant);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            EnsureFolder(TestRoot);
            yield return null;

            // Create blocker file that prevents folder creation
            string absoluteBlocker = GetAbsolutePath(retryFolder);
            File.WriteAllText(absoluteBlocker, "block");
            AssetDatabase.ImportAsset(retryFolder, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Verify blocker is in place
            Assert.IsTrue(
                File.Exists(absoluteBlocker),
                "Blocker file should exist before testing folder creation failure"
            );

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/Retry'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/Retry'")
            );

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();

            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolder),
                "Retry folder should not exist while blocker is present"
            );
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(retryAsset) == null,
                "Retry asset should not exist while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolderVariant),
                "Variant folder should not be created"
            );

            // Remove the blocker file completely and reset retry state for fresh retries
            CleanupRetryTestState(retryFolder, retryAsset, blockerMeta, retryFolderVariant);
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Verify blocker is gone
            Assert.IsFalse(
                File.Exists(absoluteBlocker),
                "Blocker file should be removed before retry"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolder),
                "Retry folder should not exist yet"
            );

            // Manually trigger ensure now that the blocker is gone - should succeed immediately
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            bool folderExists = AssetDatabase.IsValidFolder(retryFolder);
            bool assetExists = AssetDatabase.LoadAssetAtPath<Object>(retryAsset) != null;
            bool variantExists = AssetDatabase.IsValidFolder(retryFolderVariant);

            string diagnostics =
                $"folderExists={folderExists}, assetExists={assetExists}, variantExists={variantExists}, "
                + $"blockerOnDisk={File.Exists(absoluteBlocker)}, metaOnDisk={File.Exists(GetAbsolutePath(blockerMeta))}";

            Assert.IsTrue(
                folderExists && assetExists && !variantExists,
                $"Retry singleton should be created once the temporary blocker is removed. Diagnostics: {diagnostics}"
            );
        }

        private void CleanupRetryTestState(
            string retryFolder,
            string retryAsset,
            string blockerMeta,
            string retryFolderVariant
        )
        {
            // Delete assets through AssetDatabase first
            AssetDatabase.DeleteAsset(retryAsset);
            if (AssetDatabase.IsValidFolder(retryFolder))
            {
                AssetDatabase.DeleteAsset(retryFolder);
            }
            if (AssetDatabase.IsValidFolder(retryFolderVariant))
            {
                AssetDatabase.DeleteAsset(retryFolderVariant);
            }

            // Then delete any remaining files on disk
            string absoluteFolder = GetAbsolutePath(retryFolder);
            string absoluteVariant = GetAbsolutePath(retryFolderVariant);
            string absoluteMeta = GetAbsolutePath(blockerMeta);

            // Delete blocker file if it exists
            if (File.Exists(absoluteFolder))
            {
                File.Delete(absoluteFolder);
            }

            // Delete blocker meta if it exists
            if (File.Exists(absoluteMeta))
            {
                File.Delete(absoluteMeta);
            }

            // Delete folder on disk if it somehow exists as a directory
            if (Directory.Exists(absoluteFolder))
            {
                Directory.Delete(absoluteFolder, true);
            }
            if (Directory.Exists(absoluteVariant))
            {
                Directory.Delete(absoluteVariant, true);
            }

            // Delete variant meta if it exists
            string variantMeta = absoluteVariant + ".meta";
            if (File.Exists(variantMeta))
            {
                File.Delete(variantMeta);
            }
        }

        [UnityTest]
        public IEnumerator DoesNotCreateAlternateFolderWhenFileConflicts()
        {
            string conflictFolder = TestRoot + "/FileBlock";
            string conflictAsset = conflictFolder + "/FileBlockSingleton.asset";
            string conflictFile = conflictFolder;
            string conflictVariant = conflictFolder + " 1";

            AssetDatabase.DeleteAsset(conflictAsset);
            if (AssetDatabase.IsValidFolder(conflictFolder))
            {
                AssetDatabase.DeleteAsset(conflictFolder);
            }
            if (AssetDatabase.IsValidFolder(conflictVariant))
            {
                AssetDatabase.DeleteAsset(conflictVariant);
            }

            DeleteFileIfExists(conflictFile);
            EnsureFolder(TestRoot);

            string absoluteParent = Path.GetDirectoryName(GetAbsolutePath(conflictFile));
            if (!string.IsNullOrEmpty(absoluteParent) && !Directory.Exists(absoluteParent))
            {
                Directory.CreateDirectory(absoluteParent);
            }

            File.WriteAllText(GetAbsolutePath(conflictFile), "block");
            AssetDatabase.ImportAsset(conflictFile);
            yield return null;

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/FileBlock'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/FileBlock'")
            );

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            }
            finally
            {
                ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            }
            yield return null;

            Assert.IsFalse(AssetDatabase.IsValidFolder(conflictFolder));
            Assert.IsFalse(AssetDatabase.IsValidFolder(conflictVariant));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(conflictAsset) == null);

            DeleteFileIfExists(conflictFile);
            if (AssetDatabase.IsValidFolder(conflictVariant))
            {
                AssetDatabase.DeleteAsset(conflictVariant);
            }
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticRetriesCanBeDisabled()
        {
            string noRetryFolder = TestRoot + "/NoRetry";
            string noRetryAsset = noRetryFolder + "/NoRetrySingleton.asset";
            string noRetryVariant = noRetryFolder + " 1";
            string blockerMeta = noRetryFolder + ".meta";

            // Reset retry state for clean test
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();

            AssetDatabase.DeleteAsset(noRetryAsset);
            if (AssetDatabase.IsValidFolder(noRetryFolder))
            {
                AssetDatabase.DeleteAsset(noRetryFolder);
            }
            if (AssetDatabase.IsValidFolder(noRetryVariant))
            {
                AssetDatabase.DeleteAsset(noRetryVariant);
            }
            DeleteFileIfExists(noRetryFolder);
            string absoluteBlockerMeta = GetAbsolutePath(blockerMeta);
            if (File.Exists(absoluteBlockerMeta))
            {
                File.Delete(absoluteBlockerMeta);
            }

            EnsureFolder(TestRoot);
            yield return null;

            string absoluteBlocker = GetAbsolutePath(noRetryFolder);
            File.WriteAllText(absoluteBlocker, "block");
            AssetDatabase.ImportAsset(noRetryFolder, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/NoRetry'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/NoRetry'")
            );

            bool originalRetrySetting = ScriptableObjectSingletonCreator.DisableAutomaticRetries;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
            }
            finally
            {
                ScriptableObjectSingletonCreator.DisableAutomaticRetries = originalRetrySetting;
            }

            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) == null,
                "Asset should not be created while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(noRetryFolder),
                "Folder should not exist while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(noRetryVariant),
                "Variant folder should not be created"
            );

            // Remove blocker
            DeleteFileIfExists(noRetryFolder);
            if (File.Exists(absoluteBlockerMeta))
            {
                File.Delete(absoluteBlockerMeta);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Automatic retries are still disabled, so nothing should be created until we run ensure manually.
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) == null,
                "Asset should not be created automatically while retries are disabled"
            );

            // Reset retry state and enable retries, then manually trigger
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            bool folderExists = AssetDatabase.IsValidFolder(noRetryFolder);
            bool assetExists = AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) != null;
            bool variantExists = AssetDatabase.IsValidFolder(noRetryVariant);

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = originalRetrySetting;

            string diagnostics =
                $"folderExists={folderExists}, assetExists={assetExists}, variantExists={variantExists}";

            Assert.IsTrue(
                folderExists && assetExists && !variantExists,
                $"NoRetry singleton should be created when manually triggered. Diagnostics: {diagnostics}"
            );
        }

        private static IEnumerable<string> AssetImportWorkerEnvironmentScenarios()
        {
            yield return "UNITY_ASSET_IMPORT_WORKER";
            yield return "UNITY_ASSETIMPORT_WORKER";
            yield return "MY_CUSTOM_UNITY_ASSET_IMPORT_WORKER_FLAG";
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void TryDeleteFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] contents = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            if (contents == null || contents.Length == 0)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static string GetAbsolutePath(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
            {
                return string.Empty;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            string normalized = assetsRelativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, normalized);
        }

        private static void DeleteFileIfExists(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
            {
                return;
            }

            if (AssetDatabase.DeleteAsset(assetsRelativePath))
            {
                return;
            }

            string absolutePath = GetAbsolutePath(assetsRelativePath);
            if (!string.IsNullOrEmpty(absolutePath) && File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            string metaPath = absolutePath + ".meta";
            if (!string.IsNullOrEmpty(metaPath) && File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }
#endif
}

// Name collision types in different namespaces
namespace A
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}

// Name collision types in different namespaces
namespace B
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Retry")]
    internal sealed class RetrySingleton : ScriptableObjectSingleton<RetrySingleton> { }

    [ScriptableSingletonPath("CreatorTests/FileBlock")]
    internal sealed class FileBlockSingleton : ScriptableObjectSingleton<FileBlockSingleton> { }

    [ScriptableSingletonPath("CreatorTests/NoRetry")]
    internal sealed class NoRetrySingleton : ScriptableObjectSingleton<NoRetrySingleton> { }

#endif
}
